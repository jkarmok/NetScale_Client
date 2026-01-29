using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.ViewModels
{
    public class NetworkStatisticViewModel : UIScreen
    {
        private long _packetsSent;
        private long _packetsReceived;
        private long _bytesSent;
        private long _bytesReceived;

        private long _lastPacketsSent;
        private long _lastPacketsReceived;
        private long _lastBytesSent;
        private long _lastBytesReceived;
        private float _lastUpdateTime;
        
        private Label _packetsSentLabel;
        private Label _packetsReceivedLabel;
        private Label _bytesSentLabel;
        private Label _bytesReceivedLabel;
        private Label _packetsSentPerSecondLabel;
        private Label _packetsReceivedPerSecondLabel;
        private Label _bytesSentPerSecondLabel;
        private Label _bytesReceivedPerSecondLabel;
        
        private readonly StringBuilder _stringBuilder = new StringBuilder(50);
        
        private static readonly string PacketsSentText = "Packets sent: ";
        private static readonly string PacketsReceivedText = "Packets received: ";
        private static readonly string BytesSentText = "Bytes sent: ";
        private static readonly string BytesReceivedText = "Bytes received: ";
        private static readonly string PacketsSentPerSecondText = "Packets/sec (sending): ";
        private static readonly string PacketsReceivedPerSecondText = "Packets/sec (receive): ";
        private static readonly string BytesSentPerSecondText = "Bytes/sec (sending): ";
        private static readonly string BytesReceivedPerSecondText = "Bytes/sec (receive): ";
        
        public NetworkStatisticViewModel()
        {
            _lastUpdateTime = Time.time;
        }
        
        public override void Initialize(VisualElement topElement, VisualElement rootElement)
        {
            base.Initialize(topElement, rootElement);
        }
        
        protected override void SetVisualElements()
        {
            base.SetVisualElements();
            _packetsSentLabel = m_TopElement.Q<Label>("packets-sent-label");
            _packetsReceivedLabel = m_TopElement.Q<Label>("packets-received-label");
            _bytesSentLabel = m_TopElement.Q<Label>("bytes-sent-label");
            _bytesReceivedLabel = m_TopElement.Q<Label>("bytes-received-label");
 
            _packetsSentPerSecondLabel = m_TopElement.Q<Label>("packets-sent-ps-label");
            _packetsReceivedPerSecondLabel = m_TopElement.Q<Label>("packets-received-ps-label");
            _bytesSentPerSecondLabel = m_TopElement.Q<Label>("bytes-sent-ps-label");
            _bytesReceivedPerSecondLabel = m_TopElement.Q<Label>("bytes-received-ps-label");
        }
        
        public void UpdateStatistics(long packetsSent, long packetsReceived, long bytesSent,
            long bytesReceived)
        {
            _packetsSent = packetsSent;
            _packetsReceived = packetsReceived;
            _bytesSent = bytesSent;
            _bytesReceived = bytesReceived;
            UpdateUI();
        }
        
        private void UpdateLabel(Label label, string prefix, long value)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append(prefix);
            _stringBuilder.Append(value);
            label.text = _stringBuilder.ToString();
        }
        
        private void UpdateLabel(Label label, string prefix, float value)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append(prefix);
            _stringBuilder.Append(value.ToString("F2"));
            label.text = _stringBuilder.ToString();
        }
        
        private void UpdateUI()
        {
            float currentTime = Time.time;
            float deltaTime = currentTime - _lastUpdateTime;

            UpdateLabel(_packetsSentLabel, PacketsSentText, _packetsSent);
            UpdateLabel(_packetsReceivedLabel, PacketsReceivedText, _packetsReceived);
            UpdateLabel(_bytesSentLabel, BytesSentText, _bytesSent);
            UpdateLabel(_bytesReceivedLabel, BytesReceivedText, _bytesReceived);

            const float minInterval = 0.1f;
            if (deltaTime > minInterval)
            {
                float packetsSentPerSecond = (_packetsSent - _lastPacketsSent) / deltaTime;
                float packetsReceivedPerSecond = (_packetsReceived - _lastPacketsReceived) / deltaTime;
                float bytesSentPerSecond = (_bytesSent - _lastBytesSent) / deltaTime;
                float bytesReceivedPerSecond = (_bytesReceived - _lastBytesReceived) / deltaTime;
                
                UpdateLabel(_packetsSentPerSecondLabel, PacketsSentPerSecondText, packetsSentPerSecond);
                UpdateLabel(_packetsReceivedPerSecondLabel, PacketsReceivedPerSecondText, packetsReceivedPerSecond);
                UpdateLabel(_bytesSentPerSecondLabel, BytesSentPerSecondText, bytesSentPerSecond);
                UpdateLabel(_bytesReceivedPerSecondLabel, BytesReceivedPerSecondText, bytesReceivedPerSecond);

                _lastPacketsSent = _packetsSent;
                _lastPacketsReceived = _packetsReceived;
                _lastBytesSent = _bytesSent;
                _lastBytesReceived = _bytesReceived;
                _lastUpdateTime = currentTime;
            }
        }
    }
}