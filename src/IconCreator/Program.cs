using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

var iconPath = "AppIcon.ico";

using var bitmap = new Bitmap(256, 256);
using var g = Graphics.FromImage(bitmap);
g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

// Gradient background circle
using var gradBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
    new Rectangle(0, 0, 256, 256),
    Color.FromArgb(52, 73, 94),
    Color.FromArgb(44, 62, 80),
    45f);
g.FillEllipse(gradBrush, 0, 0, 256, 256);

// "CS" text - Code Switcher
using var font = new Font("Segoe UI", 80, FontStyle.Bold);
using var textBrush = new SolidBrush(Color.FromArgb(255, 107, 53));
var text = "CS";
var textSize = g.MeasureString(text, font);
var x = (256 - textSize.Width) / 2;
var y = (256 - textSize.Height) / 2 - 5;
g.DrawString(text, font, textBrush, x, y);

// Small switch icon below
using var switchFont = new Font("Segoe UI", 24);
using var switchBrush = new SolidBrush(Color.FromArgb(236, 240, 241));
g.DrawString("⇄", switchFont, switchBrush, 85, 170);

// Save as PNG first
bitmap.Save("AppIcon.png", ImageFormat.Png);

// Convert to ICO
using var fs = new FileStream(iconPath, FileMode.Create);
var ms = new System.IO.MemoryStream();
bitmap.Save(ms, ImageFormat.Png);
ms.Position = 0;

// ICO header
byte[] header = new byte[6];
header[0] = 0; header[1] = 0; // Reserved
header[2] = 1; header[3] = 0; // Type: 1 = icon
header[4] = 1; header[5] = 0; // Number of images
fs.Write(header, 0, 6);

// Directory entry
byte[] entry = new byte[16];
entry[0] = 0; // Width (0 = 256)
entry[1] = 0; // Height (0 = 256)
entry[2] = 0; // Colors
entry[3] = 0; // Reserved
entry[4] = 32; // Color planes
entry[5] = 0;
entry[6] = 32; // Bits per pixel
entry[7] = 0;
byte[] pngData = ms.ToArray();
BitConverter.GetBytes(pngData.Length).CopyTo(entry, 8);
BitConverter.GetBytes(22).CopyTo(entry, 12);
fs.Write(entry, 0, 16);

// PNG data
fs.Write(pngData, 0, pngData.Length);

Console.WriteLine($"Icon created: {Path.GetFullPath(iconPath)}");
