﻿using OpenBullet2.Core.Entities;
using OpenBullet2.Core.Repositories;
using RuriLib.Models.Data.DataPools;
using RuriLib.Models.Hits;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenBullet2.Core.Services
{
    /// <summary>
    /// Stores hits to an <see cref="IHitRepository"/> in a thread-safe manner.
    /// </summary>
    public class HitStorageService : IDisposable
    {
        private readonly IHitRepository hitRepo;
        private readonly SemaphoreSlim semaphore;

        public HitStorageService(IHitRepository hitRepo)
        {
            this.hitRepo = hitRepo;
            semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Stores a hit in a thread-safe manner.
        /// </summary>
        public async Task Store(Hit hit)
        {
            var entity = new HitEntity
            {
                CapturedData = hit.CapturedDataString,
                Data = hit.DataString,
                Date = hit.Date,
                Proxy = hit.ProxyString,
                Type = hit.Type,
                ConfigId = hit.Config.Id,
                ConfigName = hit.Config.Metadata.Name,
                ConfigCategory = hit.Config.Metadata.Category,
                OwnerId = hit.OwnerId
            };

            if (hit.DataPool is WordlistDataPool)
            {
                var wordlist = (hit.DataPool as WordlistDataPool).Wordlist;
                entity.WordlistId = wordlist.Id;
                entity.WordlistName = wordlist.Name;
            }

            // Only allow saving one hit at a time (multiple threads should
            // not use the same DbContext at the same time).
            await semaphore.WaitAsync();

            try
            {
                await hitRepo.Add(entity);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void Dispose()
        {
            semaphore?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
