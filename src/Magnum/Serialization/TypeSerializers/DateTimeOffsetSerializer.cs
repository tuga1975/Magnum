// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Magnum.Serialization.TypeSerializers
{
	using System;
	using System.Xml;
	using Extensions;

	public class DateTimeOffsetSerializer :
		TypeSerializer<DateTimeOffset>
	{
		public TypeReader<DateTimeOffset> GetReader()
		{
			return ParseShortestXsdDateTime;
		}

		public TypeWriter<DateTimeOffset> GetWriter()
		{
			return GetDateTimeOffsetString;
		}

		private static void GetDateTimeOffsetString(DateTimeOffset value, Action<string> output)
		{
			output(XmlConvert.ToString(value.ToUniversalTime()));
		}

		private static DateTimeOffset ParseShortestXsdDateTime(string text)
		{
			if (text.IsEmpty())
				return DateTimeOffset.MinValue;

			return XmlConvert.ToDateTimeOffset(text);
		}
	}
}