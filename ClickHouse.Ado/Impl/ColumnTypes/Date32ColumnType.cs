﻿using System;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using ClickHouse.Ado.Impl.ATG.Insert;
using ClickHouse.Ado.Impl.Data;
using Buffer = System.Buffer;

namespace ClickHouse.Ado.Impl.ColumnTypes
{
    internal class Date32ColumnType : DateColumnType
    {
        private static readonly DateTime UnixTimeBase = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly string Date32ColumnTypeAsClickHouseType = "Date32";

        public Date32ColumnType()
        {
        }

        public Date32ColumnType(DateTime[] data) : base(data) { }

        public override int Rows => Data?.Length ?? 0;

        internal override void Read(ProtocolFormatter formatter, int rows)
        {
#if CLASSIC_FRAMEWORK
            var itemSize = sizeof(int);
#else
            var itemSize = Marshal.SizeOf<int>();
#endif
            var bytes = formatter.ReadBytes(itemSize * rows);
            var xdata = new int[rows];
            Buffer.BlockCopy(bytes, 0, xdata, 0, itemSize * rows);
            Data = xdata.Select(x => ParseValue(x)).ToArray();
        }

        public override void Write(ProtocolFormatter formatter, int rows)
        {
            Debug.Assert(Rows == rows, "Row count mismatch!");
            foreach (var d in Data)
                formatter.WriteBytes(BitConverter.GetBytes((int)((d - UnixTimeBase).TotalDays)));
        }

        public override string AsClickHouseType(ClickHouseTypeUsageIntent usageIntent)
        {
            return Date32ColumnTypeAsClickHouseType;
        }

        public override void ValueFromConst(Parser.ValueType val)
        {
            if (val.TypeHint == Parser.ConstType.String)
                Data = new[] {
                    DateTime.ParseExact(
                        ProtocolFormatter.UnescapeStringValue(val.StringValue),
                        new[] { "yyyy-MM-dd" },
                        null,
                        DateTimeStyles.AssumeLocal
                    )
                };
            else
                throw new InvalidCastException("Cannot convert numeric value to DateTime.");
        }

        public override void ValueFromParam(ClickHouseParameter parameter)
        {
            if (parameter.DbType == DbType.Date || parameter.DbType == DbType.DateTime || parameter.DbType == DbType.DateTime2 || parameter.DbType == DbType.DateTimeOffset)
                Data = new[] { (DateTime)Convert.ChangeType(parameter.Value, typeof(DateTime)) };
            else throw new InvalidCastException($"Cannot convert parameter with type {parameter.DbType} to DateTime.");
        }

        private DateTime ParseValue(int value)
        {
            return UnixTimeBase.AddDays(value);
        }
    }
}
