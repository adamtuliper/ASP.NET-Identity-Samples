using System.Web;
using System.Web.Mvc;

namespace Local_Auth_v2._0_Complete
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
