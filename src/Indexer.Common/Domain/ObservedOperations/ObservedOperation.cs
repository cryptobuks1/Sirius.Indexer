
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Internal;
using Swisschain.Sirius.Indexer.MessagingContract;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.ObservedOperations
{
    public class ObservedOperation
    {
        private ObservedOperation(
            long operationId, 
            string blockchainId, 
            string transactionId,
            bool isCompleted,
            long assetId,
            Guid bilV1OperationId,
            IReadOnlyCollection<Unit> fees,
            string destinationAddress,
            decimal operationAmount)
        {
            OperationId = operationId;
            BlockchainId = blockchainId;
            TransactionId = transactionId;
            IsCompleted = isCompleted;
            AssetId = assetId;
            BilV1OperationId = bilV1OperationId;
            DestinationAddress = destinationAddress;
            OperationAmount = operationAmount;
            if (fees != null && fees.Any())
                Fees.AddRange(fees);
        }

        public string BlockchainId { get; }
        
        public long OperationId { get; }

        public long AssetId { get; }

        public string TransactionId { get; }

        public bool IsCompleted { get; private set; }

        public Guid BilV1OperationId { get; }

        public string DestinationAddress { get; }

        public decimal OperationAmount { get; }

        public List<Unit> Fees { get; } = new List<Unit>();

        public List<object> Events { get; } = new List<object>();

        public static ObservedOperation Create(
            long operationId,
            string blockchainId,
            string transactionId,
            long assetId,
            Guid bilV1OperationId,
            string destinationAddress,
            decimal operationAmount
            )
        {
            return new ObservedOperation(
                operationId,
                blockchainId,
                transactionId,
                false,
                assetId,
                bilV1OperationId,
                null,
                destinationAddress,
                operationAmount);
        }

        public static ObservedOperation Restore(
            long operationId,
            string blockchainId,
            string transactionId,
            bool isCompleted,
            long assetId,
            Guid bilV1OperationId,
            IReadOnlyCollection<Unit> fees,
            string destinationAddress,
            decimal operationAmount)
        {
            return new ObservedOperation(
                operationId,
                blockchainId,
                transactionId,
                isCompleted,
                assetId,
                bilV1OperationId,
                fees,
                destinationAddress,
                operationAmount);
        }

        public void Complete(long blockNumber, Unit[] fees)
        {
            if (fees != null && fees.Any())
                Fees.AddRange(fees);

            IsCompleted = true;

            var detectedTransaction = new TransactionDetected()
            {
                BlockchainId = this.BlockchainId,
                BlockId = "some block",
                BlockNumber = blockNumber,
                Fees = new List<Unit>(),
                TransactionId = this.TransactionId,
                TransactionNumber = 0,
                Error = null,
                Sources = Array.Empty<TransferSource>(),
                OperationId = this.OperationId,
                Destinations = new List<TransferDestination>()
                {
                    new TransferDestination()
                    {
                        TransferId = "0",
                        Address = DestinationAddress,
                        Unit = new Unit(this.AssetId, this.OperationAmount)
                    }
                }
            };

            Events.Add(detectedTransaction);
        }

        public void Fail(long transactionBlock, Unit[] fees, TransactionError transactionError)
        {
            if (fees != null && fees.Any())
                Fees.AddRange(fees);

            IsCompleted = true;

            var detectedTransaction = new TransactionDetected()
            {
                BlockchainId = this.BlockchainId,
                BlockId = "some block",
                BlockNumber = transactionBlock,
                Fees = new List<Unit>(),
                TransactionId = this.TransactionId,
                TransactionNumber = 0,
                Error = transactionError,
                Sources = Array.Empty<TransferSource>(),
                OperationId = this.OperationId,
                Destinations = new List<TransferDestination>()
                {
                    new TransferDestination()
                    {
                        TransferId = "0",
                        Address = DestinationAddress,
                        Unit = new Unit(this.AssetId, this.OperationAmount)
                    }
                },
            };

            Events.Add(detectedTransaction);
        }
    }
}
