using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace QASmartClass.Services
{
    public class GateMeshService
    {
        private static readonly Lazy<GateMeshService> _instance = new(() => new GateMeshService());
        public static GateMeshService Instance => _instance.Value;

        private readonly ConcurrentQueue<GateMeshMessage> _broadcastQueue = new();
        private readonly ConcurrentQueue<GateMeshMessage> _receivedQueue = new();
        private readonly ConcurrentDictionary<string, byte> _processedMessageIds = new();

        private bool _isMeshActive = true;

        private GateMeshService() { }

        public bool IsMeshActive
        {
            get => _isMeshActive;
            set => _isMeshActive = value;
        }

        public void BroadcastSwipe(string gateId, string studentCode, string direction, DateTime timestamp)
        {
            if (!_isMeshActive) return;

            string messageId = $"{studentCode}_{direction}_{timestamp.Ticks}";
            if (_processedMessageIds.TryAdd(messageId, 0))
            {
                var msg = new GateMeshMessage
                {
                    MessageId = messageId,
                    SenderGateId = gateId,
                    StudentCode = studentCode,
                    Direction = direction,
                    Timestamp = timestamp
                };
                _broadcastQueue.Enqueue(msg);
            }
        }

        public bool SimulateReceive(out GateMeshMessage message)
        {
            return _receivedQueue.TryDequeue(out message);
        }

        public void QueueReceivedMessage(GateMeshMessage msg)
        {
            if (!_isMeshActive) return;

            if (_processedMessageIds.TryAdd(msg.MessageId, 0))
            {
                _receivedQueue.Enqueue(msg);
            }
        }

        public List<GateMeshMessage> GetBroadcastedMessages()
        {
            var list = new List<GateMeshMessage>();
            while (_broadcastQueue.TryDequeue(out var msg))
            {
                list.Add(msg);
            }
            return list;
        }

        public void Clear()
        {
            _broadcastQueue.Clear();
            _receivedQueue.Clear();
            _processedMessageIds.Clear();
            _isMeshActive = true;
        }
    }

    public class GateMeshMessage
    {
        public string MessageId { get; set; } = string.Empty;
        public string SenderGateId { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty; // GateCheckIn, GateCheckOut
        public DateTime Timestamp { get; set; }
    }
}
