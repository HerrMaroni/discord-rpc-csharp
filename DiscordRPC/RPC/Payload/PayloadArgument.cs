using DiscordRPC.Helper;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscordRPC.RPC.Payload
{
	/// <summary>
	/// The payload that is sent by the client to discord for events such as setting the rich presence.
	/// <para>
	/// SetPrecense
	/// </para>
	/// </summary>
	internal class ArgumentPayload<T> : IPayload
		where T : class
	{
		/// <summary>
		/// The data the server sent too us
		/// </summary>
		[JsonPropertyName("args")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public JsonDocument Arguments { get; set; }
		
		public ArgumentPayload() { Arguments = null; }
		public ArgumentPayload(long nonce) : base(nonce) { Arguments = null; }
		public ArgumentPayload(T args, long nonce) : base(nonce)
		{
			SetObject(args);
		}

		/// <summary>
		/// Sets the obejct stored within the data.
		/// </summary>
		/// <param name="obj"></param>
		public void SetObject(T obj)
		{
			Arguments = JsonSerializer.SerializeToDocument(obj, typeof(T), JsonSerializationContext.Default);
		}

		/// <summary>
		/// Gets the object stored within the Data
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetObject()
		{
			return (T)Arguments.Deserialize(typeof(T), JsonSerializationContext.Default);
		}

		public override string ToString()
		{
			return "Argument " + base.ToString();
		}
	}
}

