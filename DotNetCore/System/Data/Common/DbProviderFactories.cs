namespace System.Data.Common
{
    internal class DbProviderFactories
    {
        internal static DbProviderFactory GetFactory(string providerName)
        {
            //System.Data.Common.DbProviderFactories.
            switch (providerName)
            {
                case "System.Data.SqlClient":
                    return System.Data.SqlClient.SqlClientFactory.Instance;

            }
                       
            throw new NotImplementedException();
        }
    }
}