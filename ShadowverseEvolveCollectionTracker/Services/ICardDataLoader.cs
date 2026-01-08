using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ShadowverseEvolveCardTracker.Models;

namespace ShadowverseEvolveCardTracker.Services
{
    public interface ICardDataLoader
    {
        /// <summary>
        /// Load all card rows from CSV files in the supplied folder path.
        /// Returns one CardData per CSV row.
        /// </summary>
        Task<IReadOnlyList<CardData>> LoadAllAsync(string folderPath, CancellationToken cancellationToken = default);
    }
}