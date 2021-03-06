﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoQueue.Core;
using MongoQueue.Core.AgentAbstractions;
using MongoQueue.Core.Entities;
using MongoQueue.Core.IntegrationAbstractions;

namespace MongoQueue
{
    public class UnprocessedMessagesAgent : IUnprocessedMessagesAgent
    {
        private readonly MongoAgent _mongoAgent;
        private readonly IMessagingConfiguration _messagingConfiguration;

        public UnprocessedMessagesAgent(
            MongoAgent mongoAgent,
            IMessagingConfiguration messagingConfiguration
        )
        {
            _mongoAgent = mongoAgent;
            _messagingConfiguration = messagingConfiguration;
        }
        public async Task<List<Envelope>> GetUnprocessed(string route, CancellationToken cancellationToken)
        {
            var threshold = DateTime.UtcNow - _messagingConfiguration.ResendThreshold;
            var collection = _mongoAgent.GetEnvelops(route);
            var notProcessedFilter = Builders<Envelope>.Filter.And(
                    Builders<Envelope>.Filter.Lt(x => x.ReadAt, threshold),
                    Builders<Envelope>.Filter.Eq(x => x.IsRead, true),
                    Builders<Envelope>.Filter.Eq(x => x.IsProcessed, false)
                );

            var notProcessed =
                await
                    (await collection.FindAsync(notProcessedFilter, cancellationToken: cancellationToken))
                        .ToListAsync(
                            cancellationToken);
            return notProcessed;
        }


        public async Task<string> Resend(string route, Envelope original, CancellationToken cancellationToken)
        {
            var collection = _mongoAgent.GetEnvelops(route);
            var resend = new Envelope(original.Topic, original.Payload, original.Id, original.ResentCount + 1);
            await collection.InsertOneAsync(resend, null, cancellationToken);
            var updateDefinition = Builders<Envelope>.Update
                .Set(x => x.ProcessedAt, DateTime.UtcNow)
                .Set(x => x.IsProcessed, true)
                .Set(x => x.ResendId, resend.Id);
            await
                collection.UpdateOneAsync(Builders<Envelope>.Filter.Eq(x => x.Id, original.Id), updateDefinition,
                    null, cancellationToken);
            return resend.Id;
        }
    }
}