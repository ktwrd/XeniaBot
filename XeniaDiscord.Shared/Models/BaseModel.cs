using MongoDB.Bson;
using System;
using System.ComponentModel;
using MongoDB.Bson.Serialization.Attributes;

namespace XeniaBot.Shared.Models
{
    [Obsolete("As of Shared v2.0, all models will be using a Guid for it's primary key (_id) instead of an ObjectId. This transition is handled by XeniaVersionService")]
    public class BaseModel : BaseModelObjectId
    {
    }
    public class BaseModelGuid : BaseModelGeneric<string>
    {
        /// <inheritdoc/>
        public override void ResetId()
        {
            InitId();
        }

        /// <summary>
        /// Initialize <see cref="Id"/>
        /// </summary>
        private void InitId()
        {
            Id = Guid.NewGuid().ToString();
        }
        public BaseModelGuid()
            : base()
        {
            InitId();
        }
    }

    /// <summary>
    /// <para>As of Shared v2.0, all models will be using a Guid for it's primary key (_id) instead of an ObjectId.</para>
    ///
    /// <para>This transition is handled by XeniaBot.Data.Services.XeniaVersionService</para>
    /// </summary>
    [Obsolete("As of Shared v2.0, all models will be using a Guid for it's primary key (_id) instead of an ObjectId. This transition is handled by XeniaVersionService")]
    public class BaseModelObjectId : BaseModelGeneric<ObjectId>
    {
        /// <inheritdoc/>
        public override void ResetId()
        {
            Id = default;
        }
    }

    public class BaseModelGeneric<T>
    {
        /// <summary>
        /// Primary Key. Must be unique in collection.
        /// </summary>
        [Browsable(false)]
        [BsonElement("_id")]
        public T Id { get; set; }

        /// <summary>
        /// Reset the value of <see cref="Id"/> to a new value.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void ResetId()
        {
            throw new NotImplementedException();
        }
    }
}
