using System;
using System.Text;
using UdonSharp;
using UnityEngine;

namespace jp.ootr.UdonBase64RLE
{
    public class UdonBase64CSVRLE : UdonSharpBehaviour
    {
        private string _input;
        private UdonSharpBehaviour _callback;
        private float _time;
        private int _index;
        private int _chunkIndex;
        private string[] _chunks;
        private StringBuilder _decoded;
        
        private readonly float _maxFrameTime = 0.01f;
        private readonly int _maxChunkSize = 1048576;

        public void DecodeAsync(UdonSharpBehaviour callback, string input)
        {
            _input = input;
            _time = Time.realtimeSinceStartup;
            _index = 0;
            _decoded = new StringBuilder();
            _callback = callback;

            var countLength = _input.IndexOf(":", StringComparison.Ordinal);
            
            var count = int.Parse(_input.Substring(0, countLength));
            _input = _input.Substring(countLength + 1);
            _chunks = new string[count];
            
            SendCustomEvent(nameof(__SplitChunks));
        }

        public void __SplitChunks()
        {
            _time = Time.realtimeSinceStartup;
            SplitChunks();
        }
        
        private void SplitChunks()
        {
            while (Time.realtimeSinceStartup - _time < _maxFrameTime && _index < _input.Length)
            {
                var rangeEnd = _index + _maxChunkSize;
                if (rangeEnd > _input.Length)
                {
                    rangeEnd = _input.Length;
                }
                else
                {
                    rangeEnd =  _input.IndexOf(",", rangeEnd, StringComparison.Ordinal);
                    if (rangeEnd == -1)
                    {
                        rangeEnd = _input.Length;
                    }
                }
                
                var split = _input.Substring(_index, rangeEnd - _index).Split(",");
                split.CopyTo(_chunks, _chunkIndex);
                _chunkIndex += split.Length;
                _index = rangeEnd + 1;
            }
            
            if (_index < _input.Length)
            {
                SendCustomEventDelayedFrames(nameof(__SplitChunks),1);
            }
            else
            {
                _index = 0;
                SendCustomEventDelayedFrames(nameof(__ParseChunk),1);
            }
        }

        public void __ParseChunk()
        {
            _time = Time.realtimeSinceStartup;
            ParseChunk();
        }
        
        private void ParseChunk()
        {
            while (Time.realtimeSinceStartup - _time < _maxFrameTime && _index < _chunks.Length)
            {
                if (!int.TryParse(_chunks[_index], out var count))
                {
                    SendCustomEvent(nameof(OnDecodeFailed));
                    return;
                }
                var data = _chunks[_index + 1];
                _index += 2;
                if (data.Length == 1)
                {
                    _decoded.Append(new string(_input[_index], count));
                    continue;
                }
                for (int i = 0; i < count; i++)
                {
                    _decoded.Append(data);
                }
            }

            if (_index < _chunks.Length)
            {
                SendCustomEventDelayedFrames(nameof(__ParseChunk),1);
            }
            else
            {
                SendCustomEvent(nameof(OnDecodeComplete));
            }
        }
        
        public void OnDecodeComplete()
        {
            _callback.SendCustomEvent("OnDecodeComplete");
        }
        
        public void OnDecodeFailed()
        {
            _callback.SendCustomEvent("OnDecodeFailed");
        }
        
        public string GetDecoded()
        {
            return _decoded.ToString();
        }
    }
}
