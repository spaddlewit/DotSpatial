﻿// Copyright (c) DotSpatial Team. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

using System;
using System.Globalization;
using System.Text;

namespace DotSpatial.Data
{
    /// <summary>
    /// Callback specified to AttributeTable.Edit() overload.
    /// </summary>
    /// <param name="e">The event args.</param>
    /// <returns>Boolean.</returns>
    public delegate bool RowEditEvent(RowEditEventArgs e);

    /// <summary>
    /// RowEditEvent arguments.
    /// </summary>
    public class RowEditEventArgs
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RowEditEventArgs"/> class.
        /// </summary>
        /// <param name="recordLength">The record length.</param>
        /// <param name="columns">The columns.</param>
        public RowEditEventArgs(int recordLength, Fields columns)
        {
            Columns = columns;
            ByteContent = new byte[recordLength];
            Modified = false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the actual byte content read from dbase.
        /// </summary>
        public byte[] ByteContent { get; set; }

        /// <summary>
        /// Gets the column information.
        /// </summary>
        public Fields Columns { get; }

        /// <summary>
        /// Gets or sets a value indicating whether one of the SetColumn methods or SetAllColumns has been called.
        /// </summary>
        public bool Modified { get; set; }

        /// <summary>
        /// Gets or sets the row number.
        /// </summary>
        public int RowNumber { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Get the entire buffer as a character array.
        /// </summary>
        /// <returns>The buffer as character array.</returns>
        public char[] GetChars()
        {
            var characterContent = new char[ByteContent.Length];
            Encoding.Default.GetChars(ByteContent, 0, ByteContent.Length, characterContent, 0);
            return characterContent;
        }

        /// <summary>
        /// Convert the all byte data into an array of data values.
        /// </summary>
        /// <returns>Array of data values.</returns>
        public object[] ParseAllColumns()
        {
            var values = new object[Columns.Count];
            for (var pos = 0; pos < values.Length; pos++)
            {
                values[pos] = ParseColumn(Columns[pos]);
            }

            return values;
        }

        /// <summary>
        /// Convert the byte data for a column into the appropriate data value.
        /// </summary>
        /// <param name="field">Column information for data value being parsed.</param>
        /// <returns>The parsed value.</returns>
        public object ParseColumn(Field field)
        {
            var cBuffer = new char[field.Length];
            Encoding.Default.GetChars(ByteContent, field.DataAddress, field.Length, cBuffer, 0);
            object tempObject = null;
            switch (field.TypeCharacter)
            {
                case 'L': // logical data type, one character (T, t, F, f, Y, y, N, n)

                    char tempChar = cBuffer[0];
                    if ((tempChar == 'T') || (tempChar == 't') || (tempChar == 'Y') || (tempChar == 'y')) tempObject = true;
                    else tempObject = false;
                    break;

                case 'C': // character record.

                    tempObject = new string(cBuffer).Trim('\0');
                    break;
                case 'T': throw new NotSupportedException();

                case 'D': // date data type.

                    var tempString = new string(cBuffer, 0, 4);
                    int year;
                    if (int.TryParse(tempString, out year) == false) break;

                    int month;
                    tempString = new string(cBuffer, 4, 2);
                    if (int.TryParse(tempString, out month) == false) break;

                    int day;
                    tempString = new string(cBuffer, 6, 2);
                    if (int.TryParse(tempString, out day) == false) break;

                    tempObject = new DateTime(year, month, day);

                    break;
                case 'F':
                case 'B':
                case 'N': // number - Esri uses N for doubles and floats

                    string tempStr = new string(cBuffer).Trim('\0').Trim();
                    tempObject = DBNull.Value;

                    switch (Type.GetTypeCode(field.DataType))
                    {
                        case TypeCode.Double:
                            tempObject = double.Parse(tempStr, NumberStyles.Number, NumberConverter.NumberConversionFormatProvider);
                            break;
                        case TypeCode.Byte:
                            tempObject = byte.Parse(tempStr);
                            break;
                        case TypeCode.Int16:
                            tempObject = short.Parse(tempStr);
                            break;
                        case TypeCode.Int32:
                            tempObject = int.Parse(tempStr);
                            break;
                        case TypeCode.Int64:
                            tempObject = long.Parse(tempStr);
                            break;
                        case TypeCode.Single:
                            tempObject = float.Parse(tempStr, NumberStyles.Number, NumberConverter.NumberConversionFormatProvider);
                            break;
                        case TypeCode.Decimal:
                            tempObject = decimal.Parse(tempStr, NumberStyles.Number, NumberConverter.NumberConversionFormatProvider);
                            break;
                    }

                    break;

                default: throw new NotSupportedException("Do not know how to parse Field type " + field.DataType);
            }

            return tempObject;
        }

        /// <summary>
        /// Convert array of values to bytes and fill ByteContent.
        /// </summary>
        /// <param name="values">The values.</param>
        public void SetAllColumns(object[] values)
        {
            int count = values.Length;
            if (count != Columns.Count) throw new ArgumentException(string.Format("Input array length: {0:D} must match number of columns: {1:D}", count, Columns.Count));

            for (int j = 0; j < count; j++)
            {
                object value = values[j];
                Field field = Columns[j];
                if (value is double) SetColumn(field, (double)value);
                else if (value is string) SetColumn(field, (string)value);
                else if (value is float) SetColumn(field, (float)value);
                else if (value == null || value is DBNull) SetColumn(field, DBNull.Value);
                else if (value is decimal) SetColumn(field, (decimal)value);
                else if (value is int || value is short || value is long || value is byte) SetColumn(field, Convert.ToInt64(value));
                else if (value is bool) SetColumn(field, (bool)value);
                else if (value is DateTime) SetColumn(field, (DateTime)value);
                else SetColumn(field, value.ToString());
            }
        }

        /// <summary>
        /// Convert value to bytes and place in ByteContent at correct location.
        /// </summary>
        /// <param name="field">Column information for the conversion.</param>
        /// <param name="value">The value.</param>
        public void SetColumn(Field field, double value)
        {
            char[] test = field.NumberConverter.ToChar(value);
            Encoding.Default.GetBytes(test, 0, test.Length, ByteContent, field.DataAddress);
            Modified = true;
        }

        /// <summary>
        /// Convert value to bytes and place in ByteContent at correct location.
        /// </summary>
        /// <param name="field">Column information for the conversion.</param>
        /// <param name="value">The value.</param>
        public void SetColumn(Field field, string value)
        {
            string text = value.PadRight(field.Length, ' ');
            string dbaseString = text.Substring(0, field.Length);
            char[] chars = dbaseString.ToCharArray();
            Encoding.Default.GetBytes(chars, 0, chars.Length, ByteContent, field.DataAddress);
            Modified = true;
        }

        /// <summary>
        /// Convert value to bytes and place in ByteContent at correct location.
        /// </summary>
        /// <param name="field">Column information for the conversion.</param>
        /// <param name="value">The value.</param>
        public void SetColumn(Field field, float value)
        {
            if (field.TypeCharacter == 'F')
            {
                SetColumn(field, value.ToString());
            }
            else
            {
                char[] test = field.NumberConverter.ToChar(value);
                Encoding.Default.GetBytes(test, 0, test.Length, ByteContent, field.DataAddress);
                Modified = true;
            }
        }

        /// <summary>
        /// Convert value to bytes and place in ByteContent at correct location.
        /// </summary>
        /// <param name="field">Column information for the conversion.</param>
        /// <param name="dbNull">The DBNull.</param>
        public void SetColumn(Field field, DBNull dbNull)
        {
            var chars = new char[field.Length];
            for (var j = 0; j < field.Length; j++) chars[j] = ' ';

            Encoding.Default.GetBytes(chars, 0, chars.Length, ByteContent, field.DataAddress);
            Modified = true;
        }

        /// <summary>
        /// Convert value to bytes and place in ByteContent at correct location.
        /// </summary>
        /// <param name="field">Column information for the conversion.</param>
        /// <param name="value">The value.</param>
        public void SetColumn(Field field, decimal value)
        {
            char[] test = field.NumberConverter.ToChar(value);
            Encoding.Default.GetBytes(test, 0, test.Length, ByteContent, field.DataAddress);
            Modified = true;
        }

        /// <summary>
        /// Convert value to bytes and place in ByteContent at correct location.
        /// </summary>
        /// <param name="field">Column information for the conversion.</param>
        /// <param name="value">The value.</param>
        public void SetColumn(Field field, long value)
        {
            string str = value.ToString();
            string text = str.PadLeft(field.Length, ' ');
            string dbaseString = text.Substring(0, field.Length);
            char[] chars = dbaseString.ToCharArray();
            Encoding.Default.GetBytes(chars, 0, chars.Length, ByteContent, field.DataAddress);
            Modified = true;
        }

        /// <summary>
        /// Convert value to bytes and place in ByteContent at correct location.
        /// </summary>
        /// <param name="field">Column information for the conversion.</param>
        /// <param name="value">The value.</param>
        public void SetColumn(Field field, bool value)
        {
            Encoding.Default.GetBytes(value ? "T" : "F", 0, 1, ByteContent, field.DataAddress);
            Modified = true;
        }

        /// <summary>
        /// Convert value to bytes and place in ByteContent at correct location.
        /// </summary>
        /// <param name="field">Column information for the conversion.</param>
        /// <param name="value">The value.</param>
        public void SetColumn(Field field, DateTime value)
        {
            SetColumn(field, value.ToString("yyyyMMdd"));
        }

        #endregion
    }
}