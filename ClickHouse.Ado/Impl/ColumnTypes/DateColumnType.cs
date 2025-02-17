﻿#pragma warning disable CS0618

using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using ClickHouse.Ado.Impl.ATG.Insert;
using ClickHouse.Ado.Impl.Data;
using Buffer = System.Buffer;

namespace ClickHouse.Ado.Impl.ColumnTypes {
    internal class DateColumnType : ColumnType {
        private static readonly DateTime UnixTimeBase = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateColumnType() { }

        public DateColumnType(DateTime[] data) => Data = data;

        public DateTime[] Data { get; protected set; }

        public override int Rows => Data?.Length ?? 0;
        internal override Type CLRType => typeof(DateTime);

        internal override void Read(ProtocolFormatter formatter, int rows) {
#if CLASSIC_FRAMEWORK
            var itemSize = sizeof(ushort);
#else
            var itemSize = Marshal.SizeOf<ushort>();
#endif
            var bytes = formatter.ReadBytes(itemSize * rows);
            var xdata = new ushort[rows];
            Buffer.BlockCopy(bytes, 0, xdata, 0, itemSize * rows);
            Data = xdata.Select(x => UnixTimeBase.AddDays(x)).ToArray();
        }

        public override string AsClickHouseType(ClickHouseTypeUsageIntent usageIntent) => "Date";

        public override void Write(ProtocolFormatter formatter, int rows) {
            Debug.Assert(Rows == rows, "Row count mismatch!");
            foreach (var d in Data)
                formatter.WriteBytes(BitConverter.GetBytes((ushort) (d - UnixTimeBase).TotalDays));
        }

        public override void ValueFromConst(Parser.ValueType val) {
            if (val.TypeHint == Parser.ConstType.String)
                Data = new[] {DateTime.ParseExact(ProtocolFormatter.UnescapeStringValue(val.StringValue), "yyyy-MM-dd", null, DateTimeStyles.AssumeLocal)};
            else
                throw new InvalidCastException("Cannot convert numeric value to Date.");
        }

        public override void ValueFromParam(ClickHouseParameter parameter) {
            if (parameter.DbType == DbType.Date || parameter.DbType == DbType.DateTime || parameter.DbType == DbType.DateTime2 || parameter.DbType == DbType.DateTimeOffset)
                Data = new[] {(DateTime) Convert.ChangeType(parameter.Value, typeof(DateTime))};
            else throw new InvalidCastException($"Cannot convert parameter with type {parameter.DbType} to Date.");
        }

        public override object Value(int currentRow) => Data[currentRow];

        public override long IntValue(int currentRow) => throw new InvalidCastException();

        public override void ValuesFromConst(IEnumerable objects) => Data = objects.Cast<DateTime>().ToArray();

        public override void NullableValuesFromConst(IEnumerable objects) => Data = objects.Cast<DateTime?>().Select(x => x.GetValueOrDefault()).ToArray();
    }
}
