using System;
using Acme.Net.Sdk.Support;
using Acme.Net.Sdk.Protocol;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Base interface for all key page operations.
    /// </summary>
    public interface IKeyPageOperation : IMarshallable
    {
        /// <summary>
        /// Gets the type of the operation.
        /// </summary>
        KeyPageOperationType Type { get; }
    }

    /// <summary>
    /// Base class for key page operations.
    /// </summary>
    public abstract class KeyPageOperation : IKeyPageOperation
    {
        /// <summary>
        /// Gets the type of the operation.
        /// </summary>
        public abstract KeyPageOperationType Type { get; }

        /// <summary>
        /// Marshals the operation to binary format.
        /// </summary>
        public abstract byte[] MarshalBinary();
    }

    /// <summary>
    /// Represents an operation to add a key to a key page.
    /// </summary>
    public class AddKeyOperation : KeyPageOperation
    {
        public override KeyPageOperationType Type => KeyPageOperationType.Add;

        /// <summary>
        /// Gets or sets the key entry to add.
        /// </summary>
        public KeySpecParams? Entry { get; set; }

        public AddKeyOperation()
        {
        }

        public AddKeyOperation(KeySpecParams entry)
        {
            Entry = entry;
        }

        public override byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();

            // Field 1: type (enum)
            marshaller.WriteUInt(1, (int)Type);

            // Field 2: entry (KeySpecParams)
            if (Entry != null)
            {
                marshaller.WriteValue(2, Entry);
            }

            return marshaller.GetBytes();
        }
    }

    /// <summary>
    /// Represents an operation to remove a key from a key page.
    /// </summary>
    public class RemoveKeyOperation : KeyPageOperation
    {
        public override KeyPageOperationType Type => KeyPageOperationType.Remove;

        /// <summary>
        /// Gets or sets the key hash to remove.
        /// </summary>
        public byte[]? KeyHash { get; set; }

        public RemoveKeyOperation()
        {
        }

        public RemoveKeyOperation(byte[] keyHash)
        {
            KeyHash = keyHash;
        }

        public override byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();

            // Field 1: type (enum)
            marshaller.WriteUInt(1, (int)Type);

            // Field 2: keyHash (bytes)
            if (KeyHash != null && KeyHash.Length > 0)
            {
                marshaller.WriteBytes(2, KeyHash);
            }

            return marshaller.GetBytes();
        }
    }

    /// <summary>
    /// Represents an operation to update a key in a key page.
    /// </summary>
    public class UpdateKeyOperation : KeyPageOperation
    {
        public override KeyPageOperationType Type => KeyPageOperationType.Update;

        /// <summary>
        /// Gets or sets the old key entry.
        /// </summary>
        public KeySpecParams? OldEntry { get; set; }

        /// <summary>
        /// Gets or sets the new key entry.
        /// </summary>
        public KeySpecParams? NewEntry { get; set; }

        public UpdateKeyOperation()
        {
        }

        public UpdateKeyOperation(KeySpecParams oldEntry, KeySpecParams newEntry)
        {
            OldEntry = oldEntry;
            NewEntry = newEntry;
        }

        public override byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();

            // Field 1: type (enum)
            marshaller.WriteUInt(1, (int)Type);

            // Field 2: oldEntry (KeySpecParams)
            if (OldEntry != null)
            {
                marshaller.WriteValue(2, OldEntry);
            }

            // Field 3: newEntry (KeySpecParams)
            if (NewEntry != null)
            {
                marshaller.WriteValue(3, NewEntry);
            }

            return marshaller.GetBytes();
        }
    }

    /// <summary>
    /// Represents an operation to set the threshold for a key page.
    /// </summary>
    public class SetThresholdKeyPageOperation : KeyPageOperation
    {
        public override KeyPageOperationType Type => KeyPageOperationType.SetThreshold;

        /// <summary>
        /// Gets or sets the threshold value.
        /// </summary>
        public int? Threshold { get; set; }

        public SetThresholdKeyPageOperation()
        {
        }

        public SetThresholdKeyPageOperation(int threshold)
        {
            Threshold = threshold;
        }

        public override byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();

            // Field 1: type (enum)
            marshaller.WriteUInt(1, (int)Type);

            // Field 2: threshold (uint)
            if (Threshold.HasValue)
            {
                marshaller.WriteUInt(2, Threshold.Value);
            }

            return marshaller.GetBytes();
        }
    }

    /// <summary>
    /// Represents an operation to update allowed transactions for a key page.
    /// </summary>
    public class UpdateAllowedKeyPageOperation : KeyPageOperation
    {
        public override KeyPageOperationType Type => KeyPageOperationType.UpdateAllowed;

        /// <summary>
        /// Gets or sets the allowed transaction type codes.
        /// </summary>
        public int[]? Allow { get; set; }

        /// <summary>
        /// Gets or sets the denied transaction type codes.
        /// </summary>
        public int[]? Deny { get; set; }

        public UpdateAllowedKeyPageOperation()
        {
        }

        public override byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();

            // Field 1: type (enum)
            marshaller.WriteUInt(1, (int)Type);

            // Field 2: allow (repeatable enum)
            if (Allow != null && Allow.Length > 0)
            {
                foreach (var txType in Allow)
                {
                    marshaller.WriteValue(2, txType);
                }
            }

            // Field 3: deny (repeatable enum)
            if (Deny != null && Deny.Length > 0)
            {
                foreach (var txType in Deny)
                {
                    marshaller.WriteValue(3, txType);
                }
            }

            return marshaller.GetBytes();
        }
    }

    /// <summary>
    /// Represents an operation to set the reject threshold for a key page.
    /// </summary>
    public class SetRejectThresholdKeyPageOperation : KeyPageOperation
    {
        public override KeyPageOperationType Type => KeyPageOperationType.SetRejectThreshold;

        /// <summary>
        /// Gets or sets the reject threshold value.
        /// </summary>
        public int? RejectThreshold { get; set; }

        public SetRejectThresholdKeyPageOperation()
        {
        }

        public SetRejectThresholdKeyPageOperation(int rejectThreshold)
        {
            RejectThreshold = rejectThreshold;
        }

        public override byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();

            // Field 1: type (enum)
            marshaller.WriteUInt(1, (int)Type);

            // Field 2: rejectThreshold (uint)
            if (RejectThreshold.HasValue)
            {
                marshaller.WriteUInt(2, RejectThreshold.Value);
            }

            return marshaller.GetBytes();
        }
    }

    /// <summary>
    /// Represents an operation to set the response threshold for a key page.
    /// </summary>
    public class SetResponseThresholdKeyPageOperation : KeyPageOperation
    {
        public override KeyPageOperationType Type => KeyPageOperationType.SetResponseThreshold;

        /// <summary>
        /// Gets or sets the response threshold value.
        /// </summary>
        public int? ResponseThreshold { get; set; }

        public SetResponseThresholdKeyPageOperation()
        {
        }

        public SetResponseThresholdKeyPageOperation(int responseThreshold)
        {
            ResponseThreshold = responseThreshold;
        }

        public override byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();

            // Field 1: type (enum)
            marshaller.WriteUInt(1, (int)Type);

            // Field 2: responseThreshold (uint)
            if (ResponseThreshold.HasValue)
            {
                marshaller.WriteUInt(2, ResponseThreshold.Value);
            }

            return marshaller.GetBytes();
        }
    }
}