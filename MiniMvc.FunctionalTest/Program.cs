using System;

namespace MiniMvc.FunctionalTest
{
    class Program
    {
        static void Main(string[] args)
        {
            WebApplication webApplication = new WebApplication();
            webApplication.Start(5000);
        }
    }
}
