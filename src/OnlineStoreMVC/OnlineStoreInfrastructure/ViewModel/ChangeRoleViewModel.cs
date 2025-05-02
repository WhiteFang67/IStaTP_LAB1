using System.Collections.Generic;

namespace OnlineStoreInfrastructure.ViewModel
{
    public class ChangeRoleViewModel
    {
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public List<string> AllRoles { get; set; }
        public IList<string> UserRoles { get; set; }

        public ChangeRoleViewModel()
        {
            AllRoles = new List<string>();
            UserRoles = new List<string>();
        }
    }
}