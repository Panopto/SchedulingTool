using System;
using System.Linq;
using PanoptoScheduleUploader.Core;
using System.IO;
using NUnit.Framework;

namespace PanoptoScheduleUploader.Test
{
    [TestFixture]
    public class WhenCsvUploaded
    {
        private readonly string _assemblyDirectory;

        public WhenCsvUploaded()
        {
            var assemblyPath = System.Reflection.Assembly.GetAssembly(typeof(WhenXmlUploaded)).Location;
            this._assemblyDirectory = Path.GetDirectoryName(assemblyPath);
        }

        [Test]
        public void AndHasAllRequiredAttributes_ThenRecordingObjectCreated()
        {
            var expectedTemplateId = new Guid("175C9F73-E974-4FCA-B8A1-170681040219");
            var csvParser = new RecorderScheduleCSVParser($"{this._assemblyDirectory}\\validCsv.csv");
            var recordings = csvParser.ExtractRecordings();

            Assert.IsTrue(recordings.All(x => !string.IsNullOrEmpty(x.Title)));
            Assert.IsTrue(recordings.Any(x => x.TemplateId == expectedTemplateId));
        }
    }
}
