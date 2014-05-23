using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using App;

namespace UnitTests
{
    [TestClass]
    public class MessageParsingTests
    {
        [TestMethod]
        public void Scenario_1()
        {
            string r = "[ACK]";
            Assert.AreEqual("", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_2()
        {
            string r = "[ACK][333";
            Assert.AreEqual("[333", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_3()
        {
            string r = "[333";
            Assert.AreEqual("[333", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_4()
        {
            string r = "333]";
            Assert.AreEqual("333]", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_5()
        {
            string r = "[aergab][333]";
            Assert.AreEqual("", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_6()
        {
            string r = "[][]";
            Assert.AreEqual("", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_7()
        {
            string r = "][";
            Assert.AreEqual("][", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_8()
        {
            string r = "]";
            Assert.AreEqual("]", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_9()
        {
            string r = "[";
            Assert.AreEqual("[", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_10()
        {
            string r = "";
            Assert.AreEqual("", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_11()
        {
            string r = "[]";
            Assert.AreEqual("", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_12()
        {
            string r = "[weewaasd][dfgs";
            r = ShoeModel.ProcessMessage(r);
            Assert.AreEqual("[dfgs", r);

            r += "ree]";
            Assert.AreEqual("", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_13()
        {
            string r = "rr][argasd]";
            Assert.AreEqual("rr]", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_14()
        {
            string r = "[rr]345[argasd]";
            Assert.AreEqual("", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_15()
        {
            string r = "234[rr]345[argasd]";
            Assert.AreEqual("", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_16()
        {
            string r = "234[rr][argasd]999";
            Assert.AreEqual("", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_17()
        {
            string r = "234[rr]asdf[argasd]999";
            Assert.AreEqual("", ShoeModel.ProcessMessage(r));
        }

        [TestMethod]
        public void Scenario_18()
        {
            string r = "rgasd]999";
            Assert.AreEqual("rgasd]", ShoeModel.ProcessMessage(r));
        }
    }
}
