// Copyright (c) Martin Costello, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MartinCostello.LondonTravel.Site.Identity;

namespace MartinCostello.LondonTravel.Site.Services.Data
{
    /// <summary>
    /// Defines a document database service.
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// Creates a new document in the store as an asynchronous operation.
        /// </summary>
        /// <param name="document">The document to create.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation to add
        /// the specified document to the store which returns the Id of the new document.
        /// </returns>
        Task<string> CreateAsync(LondonTravelUser document);

        /// <summary>
        /// Deletes the document with the specified Id as an asynchronous operation.
        /// </summary>
        /// <param name="id">The Id of the document to delete.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation to delete the
        /// document with the specified Id which returns <see langword="true"/> if the document
        /// was deleted or <see langword="false"/> if not found.
        /// </returns>
        Task<bool> DeleteAsync(string id);

        /// <summary>
        /// Gets the document with the specified Id as an asynchronous operation.
        /// </summary>
        /// <param name="id">The Id of the document to retrieve.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation to get the
        /// document with the specified Id of the specified type or <see langword="null"/> if not found.
        /// </returns>
        Task<LondonTravelUser?> GetAsync(string id);

        /// <summary>
        /// Gets any documents that match the specified predicate as an asynchronous operation.
        /// </summary>
        /// <param name="predicate">A predicate to use to match documents of interest.</param>
        /// <param name="cancellationToken">The cancellation token to use.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation to query the
        /// document store for matching documents of the specified type.
        /// </returns>
        Task<IEnumerable<LondonTravelUser>> GetAsync(
            Expression<Func<LondonTravelUser, bool>> predicate,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the count of documents in the collection as an asynchronous operation.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation to get the count of documents in the collection.
        /// </returns>
        Task<long> GetDocumentCountAsync();

        /// <summary>
        /// Replaces the document with the specified Id with the
        /// specified new document as an asynchronous operation.
        /// </summary>
        /// <param name="document">The replacement document.</param>
        /// <param name="etag">The ETag associated with the document being updated.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> representing the operation to replace the document
        /// which returns the updated document if it was successfully replaced or
        /// <see langword="null"/> if the replacement failed due to a write conflict.
        /// </returns>
        Task<LondonTravelUser?> ReplaceAsync(LondonTravelUser document, string? etag);
    }
}
