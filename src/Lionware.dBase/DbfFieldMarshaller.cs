namespace Lionware.dBase;

internal delegate DbfField DbfFieldReader(ReadOnlySpan<byte> source, IDbfContext context);
internal delegate void DbfFieldWriter(in DbfField field, Span<byte> target, IDbfContext context);

internal record class DbfFieldMarshaller(DbfFieldReader Read, DbfFieldWriter Write);
