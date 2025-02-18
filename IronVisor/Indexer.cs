using System;

namespace IronVisor {
	public class Indexer<IndexT, ValueT> {
		readonly Func<IndexT, ValueT> Getter;
		readonly Action<IndexT, ValueT> Setter;
		
		internal Indexer(Func<IndexT, ValueT> getter, Action<IndexT, ValueT> setter) {
			Getter = getter;
			Setter = setter;
		}

		public ValueT this[IndexT index] {
			get => Getter(index);
			set => Setter(index, value);
		}
	}
}