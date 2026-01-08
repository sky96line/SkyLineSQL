namespace SkyLineSQL.Visitors
{
    public record TableRef(
           string Database,
           string Schema,
           string Table,
           string FullName
       );
}
