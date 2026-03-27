/*
    Xenia Bot Project
    Copyright (C) 2026 Kate Ward (https://kate.pet)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
namespace XeniaBot.Shared;

/// <summary>
/// Interface for cleanly converting one data type, to another data type.
/// </summary>
/// <typeparam name="TSource">Input data type.</typeparam>
/// <typeparam name="TTarget">Output data type.</typeparam>
public interface IMapper<in TSource, out TTarget>
    where TSource : notnull
    where TTarget : notnull
{
    /// <summary>
    /// Map the data in the <paramref name="source"/> from type <typeparamref name="TSource"/> to a new instance with type of <typeparamref name="TTarget"/>.
    /// </summary>
    /// <param name="source">
    /// Source instance to map from.
    /// </param>
    /// <returns>Mapped object. </returns>
    public TTarget Map(TSource source);
}

/// <summary>
/// Interface for a class that would map the <typeparamref name="TSource"/> to a new copy of
/// <typeparamref name="TTarget"/>, that inherits one or more properties from an existing instance.
/// </summary>
public interface IMapperMerger<in TSource, TTarget>
    where TSource : notnull
    where TTarget : notnull
{
    /// <summary>
    /// Interface for a class that would map the <paramref name="mapSource"/> to a new copy of
    /// <typeparamref name="TTarget"/>, that inherits one or more properties from an existing instance, <paramref name="existing"/>.
    /// </summary>
    /// <param name="existing">Instance to inherit one or more properties from when creating the new instance.</param>
    /// <param name="mapSource">
    /// <inheritdoc cref="IMapper{TSource, TTarget}.Map(TSource)" path="/param[@name='source']"/>
    /// </param>
    /// <returns>
    /// New instance that may include one or more properties from the <paramref name="existing"/> instance.
    /// </returns>
    public TTarget Map(TTarget existing, TSource mapSource);
}