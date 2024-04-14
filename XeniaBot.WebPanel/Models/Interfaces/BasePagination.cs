using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Discord;
using XeniaBot.WebPanel.Helpers;

namespace XeniaBot.WebPanel.Models;

public class BasePagination<T> where T : IEquatable<T>
{
    public IEnumerable<T> Items { get; set; }
    public int Cursor { get; set; }
    public bool IsLastPage => Items.Count() < PageSize;
    public const int PageSize = 10;

    public bool IsItemLast(T item)
    {
        if (Items.Count() < 2)
            return true;

        return Items.ElementAt(Items.Count() - 1).Equals(item);
    }

    public List<T> Paginate<TValue>(IEnumerable<T> data, Func<T, TValue> selector, int page)
    {
        return AspHelper.Paginate<T, TValue>(data, selector, page, PageSize);
    }

    public List<T> Paginate<TValue>(
        IEnumerable<T> data,
        Func<IEnumerable<T>, IEnumerable<T>> logic,
        int page)
    {
        return AspHelper.Paginate<T>(data, logic, page, PageSize);
    }
}