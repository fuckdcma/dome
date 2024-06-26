﻿using SimpleFFmpegGUI.FFmpegLib;
using AudioCodec = SimpleFFmpegGUI.FFmpegLib.AudioCodec;

namespace SimpleFFmpegGUI.FFmpegArgument
{
    public class AudioArgumentsGenerator : ArgumentsGeneratorBase
    {
        /// <summary>
        /// Mã hóa
        /// </summary>
        public AudioCodec AudioCodec { get; private set; }

        /// <summary>
        /// Bitrate
        /// </summary>
        /// <param name="kb"></param>
        /// <returns></returns>
        public AudioArgumentsGenerator Bitrate(double? kb)
        {
            if (kb.HasValue)
            {
                arguments.Add(AudioCodec.Bitrate(kb.Value));
            }
            return this;
        }

        /// <summary>
        /// Audio codec
        /// </summary>
        /// <param name="codec"></param>
        /// <returns></returns>
        public AudioArgumentsGenerator Codec(string codec)
        {
            codec = codec.ToLower();
            foreach (var c in AudioCodec.AudioCodecs)
            {
                if (c.Name.ToLower() == codec || c.Lib.ToLower() == codec)
                {
                    AudioCodec = c;
                    arguments.Add(new FFmpegArgumentItem("c:a", c.Lib));
                    return this;
                }
            }
            AudioCodec = new GeneralAudioCodec();
            if (codec is not ("Tự động" or "auto") && !string.IsNullOrEmpty(codec))
            {
                arguments.Add(new FFmpegArgumentItem("c:a", codec));
            }
            return this;
        }

        /// <summary>
        /// sao chép Audio codec
        /// </summary>
        /// <returns></returns>
        public AudioArgumentsGenerator Copy()
        {
            arguments.Add(new FFmpegArgumentItem("c:a", "copy"));
            return this;
        }

        /// <summary>
        /// Vô hiệu hóa luồng âm thanh
        /// </summary>
        /// <returns></returns>
        public AudioArgumentsGenerator Disable()
        {
            arguments.Add(new FFmpegArgumentItem("an"));
            return this;
        }

        public AudioArgumentsGenerator SamplingRate(int? hz)
        {
            if (hz.HasValue)
            {
                arguments.Add(AudioCodec.SamplingRate(hz.Value));
            }
            return this;
        }
    }
}
