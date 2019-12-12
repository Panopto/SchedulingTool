using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PanoptoScheduleUploader.Core;
using System.IO;

namespace PanoptoScheduleUploader.Test
{
    [TestClass]
    public class WhenXmlUploaded
    {
        string assemblyDirectory;

        public WhenXmlUploaded()
        {
            var assemblyPath = System.Reflection.Assembly.GetAssembly(typeof(WhenXmlUploaded)).Location;
            this.assemblyDirectory = Path.GetDirectoryName(assemblyPath);
        }

        [TestMethod]
        public void AndHasAllRequiredAttributes_ThenRecordingObjectCreated()
        {
            var xmlParser = new RecorderScheduleXmlParser(string.Format("{0}\\validXml.xml", this.assemblyDirectory));
            var recordings = xmlParser.ExtractRecordings();

            Assert.IsTrue(recordings.All(x => !string.IsNullOrEmpty(x.Title)));
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void AndRequiredAttributesMissing_ThenElementListedAsInvalid()
        {
            var xmlParser = new RecorderScheduleXmlParser(string.Format("{0}\\validXml_MissingAttribute.xml", this.assemblyDirectory));
            var recordings = xmlParser.ExtractRecordings();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void AndRequiredAttributesEmpty_ThenElementListedAsInvalid()
        {
            var xmlParser = new RecorderScheduleXmlParser(string.Format("{0}\\validXml_EmptyAttribute.xml", this.assemblyDirectory));
            var recordings = xmlParser.ExtractRecordings();
        }
    }
}
