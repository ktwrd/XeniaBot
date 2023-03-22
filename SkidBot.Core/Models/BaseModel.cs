using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Models
{
    public class BaseModel
    {
        [Browsable(false)]
        public ObjectId _id { get; set; }
    }
}
