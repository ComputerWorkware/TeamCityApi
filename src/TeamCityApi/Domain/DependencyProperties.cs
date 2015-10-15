using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamCityApi.Domain
{
    public class DependencyProperties
    {
        public string Count { get; set; }

        public List<DependencyProperty> Property { get; set; }
    }
}
