using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Shared
{
    /// <summary>
    /// LockState is used to decide weather a value (or identifier), that is grouped by a key/bucket, should have it's access prevented.
    ///
    /// This can be extremely useful to ignore the emitting of events (like _discord_UserBanned) that add records to the database, when it's already been added by a handler.
    /// For example, the key could be the Guild Id (ulong) and the value could be the User Id (ulong as well).
    ///
    /// A really good example of this is <see cref="XeniaBot.Moderation.Services.ModerationService"/>, specifically the `BanMember` and `UnbanMember` methods.
    /// </summary>
    /// <typeparam name="T">Type of the key. Must never be null.</typeparam>
    /// <typeparam name="TH">Type of the value. Must never be null.</typeparam>
    public class LockState<T, TH>
        where T  : notnull
        where TH : notnull
    {
        private Dictionary<T, List<TH>> _data { get; set; }
        public LockState()
        {
            _data = new Dictionary<T, List<TH>>();
        }

        /// <summary>
        /// Called after <see cref="Lock"/> has modified the local lock state.
        /// </summary>
        public event LockStateItemDelegate<T, TH>? ItemLocked;
        /// <summary>
        /// Called after <see cref="Unlock"/> has modified the local lock state.
        /// </summary>
        public event LockStateItemDelegate<T, TH>? ItemUnlocked; 
        /// <summary>
        /// Lock an item.
        /// </summary>
        /// <param name="key">Key the item belongs to.</param>
        /// <param name="value">Value that should be locked.</param>
        /// <returns>`false` when key+value is already locked.</returns>
        public bool Lock(T key, TH value)
        {
            if (IsLocked(key, value))
                return false;

            lock (_data)
            {
                _data.TryAdd(key, new List<TH>());
                _data[key].Add(value);
            }
            ItemLocked?.Invoke(key, value);

            return true;
        }
        /// <summary>
        /// Unlock an item
        /// </summary>
        /// <param name="key">Key the value belongs to.</param>
        /// <param name="value">Value that should be unlocked.</param>
        public void Unlock(T key, TH value)
        {
            lock (_data)
            {
                _data.TryAdd(key, new List<TH>());
                _data[key] = _data[key].Where(v => v.Equals(value)).ToList();
            }
            ItemUnlocked?.Invoke(key, value);
        }
        public bool IsLocked(T key, TH value)
        {
            lock(_data)
            {
                _data.TryAdd(key, new List<TH>());
                if (_data.TryGetValue(key, out var x))
                {
                    var first = x.Where(v => v.Equals(value));
                    return first.Any();
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
