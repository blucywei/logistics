//------------------------------------------------------------------------------
// <auto-generated>
//    此代码是根据模板生成的。
//
//    手动更改此文件可能会导致应用程序中发生异常行为。
//    如果重新生成代码，则将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace Logistics.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class vw_lg_users
    {
        public string platform { get; set; }
        public int id { get; set; }
        public string card_number { get; set; }
        public string name { get; set; }
        public string car_no { get; set; }
        public string phone_no { get; set; }
        public string short_phone_no { get; set; }
        public string email { get; set; }
        public string duty { get; set; }
        public string password { get; set; }
        public Nullable<int> forbit_flag { get; set; }
        public Nullable<int> fail_times { get; set; }
        public Nullable<System.DateTime> last_login_date { get; set; }
        public string default_loc { get; set; }
        public Nullable<int> default_loc_priority { get; set; }
        public Nullable<int> limit_qty { get; set; }
        public string route { get; set; }
    }
}