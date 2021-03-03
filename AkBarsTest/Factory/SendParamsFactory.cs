using AkBarsTest.ValueObject;
using System;
using System.IO;

namespace AkBarsTest.Factory
{
	class SendParamsFactory
	{
		public SendParams Create(string sourcePath, string destinationPath)
		{
			if (sourcePath == "" || destinationPath == "") {
				throw new ArgumentException("Path can not be empty");
            }
			if (sourcePath.EndsWith("\\") || destinationPath.EndsWith("\\")) {
				throw new ArgumentException("You are not allowed to place backslash in the end of the path");
			}

			return new SendParams(sourcePath, destinationPath);
		}
	}
}
