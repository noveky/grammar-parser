namespace SyntaxParser.Shared
{
	public static class ObjectExtentions
	{
		public static bool IsIn(this object? obj, params object?[]? collection) => collection?.Contains(obj) is true;
		public static T? As<T>(this object? obj) => (T?)obj;
		public static IEnumerable<T?>? AsEnumerable<T>(this object? obj) => obj.As<IEnumerable<T?>?>();
		public static string? AsString(this object? obj) => obj.As<string?>();
		public static char? AsChar(this object? obj) => obj.As<char?>();
		public static int? AsInt(this object? obj) => obj.As<int?>();
		public static long? AsLong(this object? obj) => obj.As<long?>();
		public static float? AsFloat(this object? obj) => obj.As<float?>();
		public static double? AsDouble(this object? obj) => obj.As<double?>();
		public static decimal? AsDecimal(this object? obj) => obj.As<decimal?>();
		public static bool? AsBool(this object? obj) => obj.As<bool?>();
		public static IEnumerable<T?> Array<T>(this T? obj) => new[] { obj };
		public static IEnumerable<T?> Array<T>(this object? obj) => new[] { obj.As<T?>() };
		public static IEnumerable<T?> ConcatBefore<T>(this IEnumerable<T?>? obj, IEnumerable<T?>? other) =>
			(obj ?? Enumerable.Empty<T?>()).Concat(other ?? Enumerable.Empty<T?>());
		public static IEnumerable<T?> ConcatBefore<T>(this object? obj, object? other) =>
			ConcatBefore(obj.AsEnumerable<T?>(), other.AsEnumerable<T?>());
		public static IEnumerable<T> AppendTo<T>(this T obj, IEnumerable<T>? other) =>
			(other ?? Enumerable.Empty<T>()).Append(obj);
		public static IEnumerable<T?> AppendTo<T>(this object? obj, object? other) =>
			AppendTo(obj.As<T?>(), other.AsEnumerable<T?>());
		public static IEnumerable<T> PrependTo<T>(this T obj, IEnumerable<T>? other) =>
			(other ?? Enumerable.Empty<T>()).Prepend(obj);
		public static IEnumerable<T?> PrependTo<T>(this object? obj, object? other) =>
			PrependTo(obj.As<T?>(), other.AsEnumerable<T?>());
	}
}
