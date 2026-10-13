/*
 File: MidiFile.cs
 Brief: A lightweight MIDI file parser included in the project to extract tracks, events and text/meta information.
*/

namespace MidiParser
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;

	/// <summary>Parses standard MIDI file headers and exposes their tracks and timing metadata.</summary>
	public class MidiFile
	{
		/// <summary>
		/// Lightweight MIDI file parser used to read tracks, events and meta information from standard MIDI files.
		/// </summary>
		public readonly int Format;

		/// <summary>Gets the number of ticks that represent one quarter note.</summary>
		public readonly int TicksPerQuarterNote;

		/// <summary>Gets the parsed tracks in the MIDI file.</summary>
		public readonly MidiTrack[] Tracks;

		/// <summary>Gets the number of tracks declared by the MIDI file.</summary>
		public readonly int TracksCount;

		/// <summary>Reads and parses a MIDI file from a stream.</summary>
		/// <param name="stream">Stream containing the MIDI file bytes.</param>
		/// <exception cref="FormatException">The stream does not contain a supported standard MIDI file.</exception>
		public MidiFile(Stream stream)
			: this(Reader.ReadAllBytesFromStream(stream))
		{ }

		/// <summary>Reads and parses a MIDI file from a path.</summary>
		/// <param name="path">Path to the MIDI file.</param>
		/// <exception cref="FormatException">The file does not contain a supported standard MIDI file.</exception>
		public MidiFile(string path)
			: this(File.ReadAllBytes(path))
		{ }

		/// <summary>Parses MIDI header and track data from a byte array.</summary>
		/// <param name="data">Complete standard MIDI file data.</param>
		/// <exception cref="FormatException">The header is invalid or uses unsupported SMPTE timing.</exception>
		public MidiFile(byte[] data)
		{
			var position = 0;

			if(Reader.ReadString(data, ref position, 4) != "MThd")
			{
				throw new FormatException("Invalid file header (expected MThd)");
			}

			if(Reader.Read32(data, ref position) != 6)
			{
				throw new FormatException("Invalid header length (expected 6)");
			}

			this.Format = Reader.Read16(data, ref position);
			this.TracksCount = Reader.Read16(data, ref position);
			this.TicksPerQuarterNote = Reader.Read16(data, ref position);

			if((this.TicksPerQuarterNote & 0x8000) != 0)
			{
				throw new FormatException("Invalid timing mode (SMPTE timecode not supported)");
			}

			this.Tracks = new MidiTrack[this.TracksCount];

			for(var i = 0; i < this.TracksCount; i++)
			{
				this.Tracks[i] = ParseTrack(i, data, ref position);
			}
		}

		/// <summary>Reads supported tempo, time-signature, and key-signature metadata into event arguments.</summary>
		/// <param name="data">MIDI file data.</param>
		/// <param name="position">Current byte offset, advanced past the event payload.</param>
		/// <param name="metaEventType">Metadata event type.</param>
		/// <param name="data1">First decoded metadata value.</param>
		/// <param name="data2">Second decoded metadata value.</param>
		/// <returns><see langword="true"/> when the metadata type is supported; otherwise, <see langword="false"/>.</returns>
		private static bool ParseMetaEvent(
			byte[] data,
			ref int position,
			byte metaEventType,
			ref byte data1,
			ref byte data2)
		{
			switch(metaEventType)
			{
			case (byte)MetaEventType.Tempo:
				var mspqn = (data[position + 1] << 16) | (data[position + 2] << 8) | data[position + 3];
				data1 = (byte)(60000000.0 / mspqn);
				position += 4;
				return true;

			case (byte)MetaEventType.TimeSignature:
				data1 = data[position + 1];
				data2 = (byte)Math.Pow(2.0, data[position + 2]);
				position += 5;
				return true;

			case (byte)MetaEventType.KeySignature:
				data1 = data[position + 1];
				data2 = data[position + 2];
				position += 3;
				return true;

			// Ignore Other Meta Events
			default:
				var length = Reader.ReadVarInt(data, ref position);
				position += length;
				return false;
			}
		}

		/// <summary>Parses one MIDI track and advances the file offset to its end.</summary>
		/// <param name="index">Zero-based track index.</param>
		/// <param name="data">Complete MIDI file data.</param>
		/// <param name="position">Current byte offset, advanced through the track.</param>
		/// <returns>The parsed track and its MIDI and text events.</returns>
		/// <exception cref="FormatException">The track header is invalid.</exception>
		private static MidiTrack ParseTrack(int index, byte[] data, ref int position)
		{
			if(Reader.ReadString(data, ref position, 4) != "MTrk")
			{
				throw new FormatException("Invalid track header (expected MTrk)");
			}

			var trackLength = Reader.Read32(data, ref position);
			var trackEnd = position + trackLength;

			var track = new MidiTrack { Index = index };
			var time = 0;
			var status = (byte)0;

			while(position < trackEnd)
			{
				time += Reader.ReadVarInt(data, ref position);

				var peekByte = data[position];

				// If the most significant bit is set then this is a status byte
				if((peekByte & 0x80) != 0)
				{
					status = peekByte;
					++position;
				}

				// If the most significant nibble is not an 0xF this is a channel event
				if((status & 0xF0) != 0xF0)
				{
					// Separate event type from channel into two
					var eventType = (byte)(status & 0xF0);
					var channel = (byte)((status & 0x0F) + 1);

					var data1 = data[position++];

					// If the event type doesn't start with 0b110 it has two bytes of data (i.e. except 0xC0 and 0xD0)
					var data2 = (eventType & 0xE0) != 0xC0 ? data[position++] : (byte)0;

					// Convert NoteOn events with 0 velocity into NoteOff events
					if(eventType == (byte)MidiEventType.NoteOn && data2 == 0)
					{
						eventType = (byte)MidiEventType.NoteOff;
					}

					track.MidiEvents.Add(
						new MidiEvent { Time = time, Type = eventType, Arg1 = channel, Arg2 = data1, Arg3 = data2 });
				}
				else
				{
					if(status == 0xFF)
					{
						// Meta Event
						var metaEventType = Reader.Read8(data, ref position);

						// There is a group of meta event types reserved for text events which we store separately
						if(metaEventType >= 0x01 && metaEventType <= 0x0F)
						{
							var textLength = Reader.ReadVarInt(data, ref position);
							var textValue = Reader.ReadString(data, ref position, textLength);
							var textEvent = new TextEvent { Time = time, Type = metaEventType, Value = textValue };
							track.TextEvents.Add(textEvent);
						}
						else
						{
							var data1 = (byte)0;
							var data2 = (byte)0;

							// We only handle the few meta events we care about and skip the rest
							if(ParseMetaEvent(data, ref position, metaEventType, ref data1, ref data2))
							{
								track.MidiEvents.Add(
									new MidiEvent
									{
										Time = time,
										Type = status,
										Arg1 = metaEventType,
										Arg2 = data1,
										Arg3 = data2
									});
							}
						}
					}
					else if(status == 0xF0 || status == 0xF7)
					{
						// SysEx event
						var length = Reader.ReadVarInt(data, ref position);
						position += length;
					}
					else
					{
						++position;
					}
				}
			}

			return track;
		}

		/// <summary>Provides low-level readers for MIDI binary values and strings.</summary>
		private static class Reader
		{
			/// <summary>Reads a big-endian 16-bit integer and advances the offset.</summary>
			/// <param name="data">Source byte array.</param>
			/// <param name="i">Byte offset to read and advance.</param>
			/// <returns>The decoded integer.</returns>
			public static int Read16(byte[] data, ref int i)
			{
				return (data[i++] << 8) | data[i++];
			}

			/// <summary>Reads a big-endian 32-bit integer and advances the offset.</summary>
			/// <param name="data">Source byte array.</param>
			/// <param name="i">Byte offset to read and advance.</param>
			/// <returns>The decoded integer.</returns>
			public static int Read32(byte[] data, ref int i)
			{
				return (data[i++] << 24) | (data[i++] << 16) | (data[i++] << 8) | data[i++];
			}

			/// <summary>Reads one byte and advances the offset.</summary>
			/// <param name="data">Source byte array.</param>
			/// <param name="i">Byte offset to read and advance.</param>
			/// <returns>The byte at the specified offset.</returns>
			public static byte Read8(byte[] data, ref int i)
			{
				return data[i++];
			}

			/// <summary>Reads all remaining bytes from a stream.</summary>
			/// <param name="input">Stream to consume.</param>
			/// <returns>Bytes read from the stream.</returns>
			public static byte[] ReadAllBytesFromStream(Stream input)
			{
				var buffer = new byte[16 * 1024];
				using(var ms = new MemoryStream())
				{
					int read;
					while((read = input.Read(buffer, 0, buffer.Length)) > 0)
					{
						ms.Write(buffer, 0, read);
					}

					return ms.ToArray();
				}
			}

			/// <summary>Reads an ASCII string of the specified length and advances the offset.</summary>
			/// <param name="data">Source byte array.</param>
			/// <param name="i">Byte offset to read and advance.</param>
			/// <param name="length">Number of bytes to decode.</param>
			/// <returns>The decoded string.</returns>
			public static string ReadString(byte[] data, ref int i, int length)
			{
				var result = Encoding.ASCII.GetString(data, i, length);
				i += length;
				return result;
			}

			/// <summary>Reads a MIDI variable-length integer and advances the offset.</summary>
			/// <param name="data">Source byte array.</param>
			/// <param name="i">Byte offset to read and advance.</param>
			/// <returns>The decoded integer.</returns>
			public static int ReadVarInt(byte[] data, ref int i)
			{
				var result = (int)data[i++];

				if((result & 0x80) == 0)
				{
					return result;
				}

				result &= 0x7F;

				for(var j = 0; j < 3; j++)
				{
					var value = (int)data[i++];

					result = (result << 7) | (value & 0x7F);

					if((value & 0x80) == 0)
					{
						break;
					}
				}

				return result;
			}
		}
	}

	/// <summary>Stores channel events and text events parsed from one MIDI track.</summary>
	public class MidiTrack
	{
		/// <summary>Gets or sets the zero-based track index.</summary>
		public int Index;

		/// <summary>Gets or sets channel and supported metadata events in this track.</summary>
		public List<MidiEvent> MidiEvents = new List<MidiEvent>();

		/// <summary>Gets or sets text metadata events in this track.</summary>
		public List<TextEvent> TextEvents = new List<TextEvent>();
	}

	/// <summary>Represents a parsed channel or supported metadata event.</summary>
	public struct MidiEvent
	{
		/// <summary>Gets or sets the event time in MIDI ticks.</summary>
		public int Time;

		/// <summary>Gets or sets the raw MIDI event status type.</summary>
		public byte Type;

		/// <summary>Gets or sets the first event argument (channel or metadata type).</summary>
		public byte Arg1;

		/// <summary>Gets or sets the second event argument.</summary>
		public byte Arg2;

		/// <summary>Gets or sets the third event argument.</summary>
		public byte Arg3;

		/// <summary>Gets the event type as a <see cref="MidiEventType"/> value.</summary>
		public MidiEventType MidiEventType => (MidiEventType)this.Type;

		/// <summary>Gets the metadata type for a metadata event.</summary>
		public MetaEventType MetaEventType => (MetaEventType)this.Arg1;

		/// <summary>Gets the one-based MIDI channel.</summary>
		public int Channel => this.Arg1;

		/// <summary>Gets the note number for note events.</summary>
		public int Note => this.Arg2;

		/// <summary>Gets the velocity for note events.</summary>
		public int Velocity => this.Arg3;

		/// <summary>Gets the control-change type for controller events.</summary>
		public ControlChangeType ControlChangeType => (ControlChangeType)this.Arg2;

		/// <summary>Gets the controller value or third event argument.</summary>
		public int Value => this.Arg3;
	}

	/// <summary>Represents text metadata extracted from a MIDI track.</summary>
	public struct TextEvent
	{
		/// <summary>Gets or sets the event time in MIDI ticks.</summary>
		public int Time;

		/// <summary>Gets or sets the raw text event type.</summary>
		public byte Type;

		/// <summary>Gets or sets the decoded text value.</summary>
		public string Value;

		/// <summary>Gets the text event type as a <see cref="TextEventType"/> value.</summary>
		public TextEventType TextEventType => (TextEventType)this.Type;
	}

	/// <summary>Defines the channel and metadata event status types recognized by the parser.</summary>
	public enum MidiEventType : byte
	{
		/// <summary>Note-off channel event.</summary>
		NoteOff = 0x80,

		/// <summary>Note-on channel event.</summary>
		NoteOn = 0x90,

		/// <summary>Key aftertouch channel event.</summary>
		KeyAfterTouch = 0xA0,

		/// <summary>Control-change channel event.</summary>
		ControlChange = 0xB0,

		/// <summary>Program-change channel event.</summary>
		ProgramChange = 0xC0,

		/// <summary>Channel aftertouch channel event.</summary>
		ChannelAfterTouch = 0xD0,

		/// <summary>Pitch-bend channel event.</summary>
		PitchBendChange = 0xE0,

		/// <summary>MIDI metadata event.</summary>
		MetaEvent = 0xFF
	}

	/// <summary>Defines recognized MIDI control-change controller numbers.</summary>
	public enum ControlChangeType : byte
	{
		/// <summary>Bank select controller.</summary>
		BankSelect = 0x00,

		/// <summary>Modulation controller.</summary>
		Modulation = 0x01,

		/// <summary>Volume controller.</summary>
		Volume = 0x07,

		/// <summary>Balance controller.</summary>
		Balance = 0x08,

		/// <summary>Pan controller.</summary>
		Pan = 0x0A,

		/// <summary>Sustain controller.</summary>
		Sustain = 0x40
	}

	/// <summary>Defines the text metadata event types recognized by the parser.</summary>
	public enum TextEventType : byte
	{
		/// <summary>General text event.</summary>
		Text = 0x01,

		/// <summary>Track-name event.</summary>
		TrackName = 0x03,

		/// <summary>Lyric event.</summary>
		Lyric = 0x05,
	}

	/// <summary>Defines tempo, time-signature, and key-signature MIDI metadata event types.</summary>
	public enum MetaEventType : byte
	{
		/// <summary>Tempo metadata event.</summary>
		Tempo = 0x51,

		/// <summary>Time-signature metadata event.</summary>
		TimeSignature = 0x58,

		/// <summary>Key-signature metadata event.</summary>
		KeySignature = 0x59
	}
}
