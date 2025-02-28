// Copyright (c) DotSpatial Team. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace DotSpatial.Data
{
    /// <summary>
    /// This is not a data class exactly, but instead is for making it easier to modify byte values of a 32 bit ARGB
    /// bitmap, but doesn't require a file.
    /// </summary>
    public class BitmapGrid : ICloneable, IEnumerable<Color>
    {
        #region Private Variables

        private Bitmap _bmp;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapGrid"/> class.
        /// </summary>
        /// <param name="source">The source to get the image data from.</param>
        public BitmapGrid(Image source)
        {
            _bmp = ConvertToBitmap(source); // ensure we have a 32bpp format bitmap.

            BitmapToBytes();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapGrid"/> class that is blank.
        /// </summary>
        /// <param name="numRows">Number of rows in the image.</param>
        /// <param name="numCols">Number of columns in the image.</param>
        public BitmapGrid(int numRows, int numCols)
        {
            _bmp = new Bitmap(numRows, numCols, PixelFormat.Format32bppArgb);
            BitmapToBytes();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the bitmap underlying this grid.
        /// Setting this will automatically replace the byte array.
        /// Getting this will automatically convert the bytes back into bitmap form.
        /// </summary>
        public Bitmap BitmapImage
        {
            get
            {
                BytesToBitmap();
                return _bmp;
            }

            set
            {
                _bmp = ConvertToBitmap(value);
                BitmapToBytes();
            }
        }

        /// <summary>
        /// Gets the integer height of the bitmap in this grid in pixels.
        /// </summary>
        public int Height
        {
            get
            {
                if (_bmp == null) return 0;
                return _bmp.Height;
            }
        }

        /// <summary>
        /// gets the integer stride, or number of actual bytes in a single row. This is not always the same as the number of columns.
        /// </summary>
        public int Stride { get; private set; }

        /// <summary>
        /// Gets or sets the array of bytes directly. Setting this can be dangerous and is not recommended. Setting individual values in the
        /// array is perfectly acceptable, since it won't interfere with the stride or number of bytes expected.
        /// </summary>
        public byte[] Values { get; set; }

        /// <summary>
        /// Gets the current width (number of columns) of the bitmap in pixels.
        /// </summary>
        public int Width
        {
            get
            {
                if (_bmp == null) return 0;
                return _bmp.Width;
            }
        }

        #endregion

        #region Methods

        #region ICloneable Members

        /// <summary>
        /// Creates a clone of this memory bitmap grid by basically cloning the bitmap itself
        /// and then building a new BitmapGrid around the clone.
        /// </summary>
        /// <returns>A clone of this bitmap grid.</returns>
        public object Clone()
        {
            return Copy();
        }

        #endregion

        #region IEnumerable<Color> Members

        /// <summary>
        /// Gets an enumerator for cycling through the color values.
        /// </summary>
        /// <returns>The enumerator for cycling through the color values.</returns>
        public IEnumerator<Color> GetEnumerator()
        {
            return new BitmapGridEnumerator(this);
        }

        /// <summary>
        /// Gets an enumerator for cycling through the color values.
        /// </summary>
        /// <returns>The enumerator for cycling through the color values.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new BitmapGridEnumerator(this);
        }

        #endregion

        /// <summary>
        /// Creates a disconnected duplicate of this BitmapGrid.
        /// </summary>
        /// <returns>A copy of this BitmapGrid.</returns>
        public BitmapGrid Copy()
        {
            // Since we are starting with the image, we need to ensure that the values are up to date
            Bitmap newBmp = BitmapImage.Clone() as Bitmap;
            BitmapGrid result = new(newBmp);
            return result;
        }

        /// <summary>
        /// Clears the byte values in the grid, replacing them with 0.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < Values.Length; i++)
            {
                Values[i] = 0;
            }
        }

        /// <summary>
        /// Disposes the unmanaged aspect of the bmp.
        /// </summary>
        public void Dispose()
        {
            _bmp.Dispose();
        }

        /// <summary>
        /// Erases over any current content in the values, and copies the byte values of the specified color in its place.
        /// </summary>
        /// <param name="fillColor">The color to fill the image with.</param>
        public void Fill(Color fillColor)
        {
            byte a = fillColor.A;
            byte r = fillColor.R;
            byte g = fillColor.G;
            byte b = fillColor.B;
            int stride = Stride;
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    Values[row * stride + col * 4] = b;
                    Values[row * stride + col * 4 + 1] = g;
                    Values[row * stride + col * 4 + 2] = r;
                    Values[row * stride + col * 4 + 3] = a;
                }
            }
        }

        /// <summary>
        /// Tests the specified bitmap to verify that it is both a Bitmap and in ARGB pixel format.
        /// If it is anything different, then this returns a newly created bitmap with a copy drawn onto it.
        /// </summary>
        /// <param name="source">Image, that gets converted to bitmap.</param>
        /// <returns>A new bitmap if the sources pixelformat differs from Format32bppArgb. Otherwise a bitmap that contains the sources image data.</returns>
        public Bitmap ConvertToBitmap(Image source)
        {
            Bitmap result;
            if (source.PixelFormat == PixelFormat.Format32bppArgb)
            {
                result = source as Bitmap;
                if (result != null) return result;
            }

            result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(result);
            g.DrawImageUnscaled(source, 0, 0);
            g.Dispose();
            return result;
        }

        /// <summary>
        /// This will calculate the difference. The width and height of the new grid will be the larger
        /// of the rows and columns between the compare grid and this grid. Values outside the range
        /// of one of the grids will simply be filled in as the value stored in the other grid. Because
        /// byte values can't be negative, the difference will be the absolute value.
        /// </summary>
        /// <param name="compare">The BitmapGrid this BitmapGrid should be compared to.</param>
        /// <returns>A BitmapGrid with byte values calculated by taking the difference between the two grids.</returns>
        public BitmapGrid Difference(BitmapGrid compare)
        {
            return DoDifference(compare, false);
        }

        /// <summary>
        /// By default, all the bands have a difference comparison done. This may not have the desired effect
        /// because the alpha channel for most colors is actually just 255. The mathematical difference would
        /// be zero, resulting in an output image that, while being the actual difference, is essentially
        /// entirely invisibile (except where the images don't overlap.)
        /// </summary>
        /// <param name="compare">The BitmapGrid this BitmapGrid should be compared to.</param>
        /// <param name="ignoreAlpha">Indicate whether the alpha value should be ignored.</param>
        /// <returns>A BitmapGrid with byte values calculated by taking the difference between the two grids.</returns>
        public BitmapGrid Difference(BitmapGrid compare, bool ignoreAlpha)
        {
            return DoDifference(compare, ignoreAlpha);
        }

        /// <summary>
        /// Gets a color structure for the specified row and column.
        /// </summary>
        /// <param name="row">The zero based integer row index to get the color from.</param>
        /// <param name="col">The zero based integer column index to get the color from.</param>
        /// <returns>A System.Color structure created from the byte values in the values array.</returns>
        public Color GetColor(int row, int col)
        {
            if (Values == null || Stride == 0) return Color.Empty;
            int b = Values[row * Stride + col * 4];
            int g = Values[row * Stride + col * 4 + 1];
            int r = Values[row * Stride + col * 4 + 2];
            int a = Values[row * Stride + col * 4 + 3];
            return Color.FromArgb(a, r, g, b);
        }

        /// <summary>
        /// Compares the bytes of this grid to the bytes of another grid. If the measurements
        /// of the other grid don't match this, then this returns false.
        /// </summary>
        /// <param name="otherGrid">The other BitmapGrid to test against.</param>
        /// <returns>Boolean, true if the bytes are the same in each case.</returns>
        public bool Matches(BitmapGrid otherGrid)
        {
            if (otherGrid.Width != Width) return false;
            if (otherGrid.Height != Height) return false;
            if (otherGrid.Stride != Stride) return false;
            byte[] otherValues = otherGrid.Values;
            for (int i = 0; i < otherValues.Length; i++)
            {
                if (Values[i] != otherValues[i]) return false;
            }

            return true;
        }

        /// <summary>
        /// Replaces all the byte values of this grid with randomly generated values.
        /// </summary>
        public void Randomize()
        {
            Random rnd = new(DateTime.Now.Millisecond);
            rnd.NextBytes(Values);
        }

        /// <summary>
        /// Sets the color structure by copying the byte ARGB values into the values array.
        /// </summary>
        /// <param name="row">The integer row index to copy values to.</param>
        /// <param name="col">The integer column index to copy values to.</param>
        /// <param name="color">The color structure to turn into bytes.</param>
        public void SetColor(int row, int col, Color color)
        {
            if (Stride == 0 || Values == null) return;
            Values[row * Stride + col * 4] = color.B;
            Values[row * Stride + col * 4 + 1] = color.G;
            Values[row * Stride + col * 4 + 2] = color.R;
            Values[row * Stride + col * 4 + 3] = color.A;
        }

        private void BitmapToBytes()
        {
            // Lockbits gets fussy if we don't have a "saved" location for the bitmap
            MemoryStream ms = new();
            _bmp.Save(ms, ImageFormat.Bmp);

            BitmapData bData = _bmp.LockBits(new Rectangle(0, 0, _bmp.Width, _bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Stride = bData.Stride;
            Values = new byte[bData.Height * bData.Stride];
            Marshal.Copy(bData.Scan0, Values, 0, bData.Height * bData.Stride);
            _bmp.UnlockBits(bData);
            ms.Close();
        }

        private void BytesToBitmap()
        {
            Rectangle bnds = new(0, 0, Width, Height);
            BitmapData bData = _bmp.LockBits(bnds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Marshal.Copy(Values, 0, bData.Scan0, Values.Length);
            _bmp.UnlockBits(bData);
        }

        private BitmapGrid DoDifference(BitmapGrid compare, bool ignoreAlpha)
        {
            int h1 = Height;
            int w1 = Width;
            int h2 = compare.Height;
            int w2 = compare.Width;

            int minHeight = Math.Min(h1, h2);
            int maxHeight = Math.Max(h1, h2);
            int minWidth = Math.Min(w1, w2);
            int maxWidth = Math.Max(w1, w2);
            BitmapGrid result = new(maxWidth, maxHeight);
            byte[] compareValues = compare.Values;
            byte[] resultValues = result.Values;
            int stride1 = Stride;
            int stride2 = compare.Stride;
            int resStride = result.Stride;
            for (int row = 0; row < maxHeight; row++)
            {
                for (int col = 0; col < maxWidth; col++)
                {
                    if (row < minHeight && col < minWidth)
                    {
                        // Both have values
                        byte a;
                        if (ignoreAlpha == false)
                        {
                            a = (byte)Math.Abs(compareValues[row * stride2 + col * 4 + 3] - Values[row * stride1 + col * 4 + 3]);
                        }
                        else
                        {
                            a = 255;
                        }

                        resultValues[row * resStride + col * 4] = (byte)Math.Abs(compareValues[row * stride2 + col * 4] - Values[row * stride1 + col * 4]);
                        resultValues[row * resStride + col * 4 + 1] = (byte)Math.Abs(compareValues[row * stride2 + col * 4 + 1] - Values[row * stride1 + col * 4 + 1]);
                        resultValues[row * resStride + col * 4 + 2] = (byte)Math.Abs(compareValues[row * stride2 + col * 4 + 2] - Values[row * stride1 + col * 4 + 2]);
                        resultValues[row * resStride + col * 4 + 3] = a;
                    }
                    else if ((row >= h1 || col >= w1) && (row >= h2 || col >= w2))
                    {
                        // neither image has values, but the difference here is zero.
                        resultValues[row * resStride + col * 4] = 0;
                        resultValues[row * resStride + col * 4 + 1] = 0;
                        resultValues[row * resStride + col * 4 + 2] = 0;
                        resultValues[row * resStride + col * 4 + 3] = 0;
                    }
                    else if (row < h1 && col < w1)
                    {
                        // This image has values here but the compare grid doesn't
                        resultValues[row * resStride + col * 4] = Values[row * stride1 + col * 4];
                        resultValues[row * resStride + col * 4 + 1] = Values[row * stride1 + col * 4 + 1];
                        resultValues[row * resStride + col * 4 + 2] = Values[row * stride1 + col * 4 + 2];
                        resultValues[row * resStride + col * 4 + 3] = Values[row * stride1 + col * 4 + 3];
                    }
                    else if (row < h2 && col < w2)
                    {
                        // The compare grid has values here but this grid doesn't
                        resultValues[row * resStride + col * 4] = compareValues[row * stride2 + col * 4];
                        resultValues[row * resStride + col * 4 + 1] = compareValues[row * stride2 + col * 4 + 1];
                        resultValues[row * resStride + col * 4 + 2] = compareValues[row * stride2 + col * 4 + 2];
                        resultValues[row * resStride + col * 4 + 3] = compareValues[row * stride2 + col * 4 + 3];
                    }
                }
            }

            return result;
        }

        #endregion

        #region Enumerator

        /// <summary>
        /// Cycles through the values of a BitmapGrid starting at the top left corner and moving in row major
        /// fashion, (raster-scan fashion, moving across end then down.)
        /// </summary>
        private class BitmapGridEnumerator : IEnumerator<Color>
        {
            private readonly int _height;
            private readonly int _stride;
            private readonly byte[] _values;
            private readonly int _width;
            private int _column;

            private int _row;

            /// <summary>
            /// Initializes a new instance of the <see cref="BitmapGridEnumerator"/> class based on the specified parentGrid.
            /// This automatically ignores any bytes in the range past the "width".
            /// </summary>
            /// <param name="parentGrid">The parent grid to cycle through the values of.</param>
            public BitmapGridEnumerator(BitmapGrid parentGrid)
            {
                _values = parentGrid.Values;
                _stride = parentGrid.Stride;
                _height = parentGrid.Height;
                _width = parentGrid.Width;
            }

            #region IEnumerator<Color> Members

            /// <summary>
            /// Gets the current color value from this grid.
            /// </summary>
            public Color Current { get; private set; }

            object IEnumerator.Current => Current;

            /// <summary>
            /// This does nothing.
            /// </summary>
            public void Dispose()
            {
                // not sure we need to do anything here
            }

            /// <summary>
            /// Advances the color to the next position.
            /// </summary>
            /// <returns>Boolean, false if there were no more cells in the image.</returns>
            public bool MoveNext()
            {
                _column += 4;
                if (_column >= _width * 4)
                {
                    _column = 0;
                    _row += 1;
                    if (_row >= _height) return false;
                }

                int b = _values[_row * _stride + _column * 4];
                int g = _values[_row * _stride + _column * 4 + 1];
                int r = _values[_row * _stride + _column * 4 + 2];
                int a = _values[_row * _stride + _column * 4 + 3];
                Current = Color.FromArgb(a, r, g, b);
                return true;
            }

            /// <summary>
            /// Resets the enumeration to the top left corner of the image.
            /// </summary>
            public void Reset()
            {
                _row = 0;
                _column = 0;
            }

            #endregion
        }

        #endregion
    }
}