using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests
{
    [TestClass]
    public class NaNBoxTests
    {
        //[TestMethod]
        public void TestBoxInt()
        { 
            for (long i = int.MinValue;i<= int.MaxValue;i+=1) 
            { 
                NaNBoxing boxing = new NaNBoxing();
                boxing.SetInt((int)i);

                Assert.AreEqual(boxing.ValueType , NaNBoxing.BoxType.Int);

                Assert.AreEqual(boxing.IntValue, i);

            }

            

        }

        //[TestMethod]
        public void TestBoxUInt()
        {
            for (long i = uint.MinValue; i <= uint.MaxValue; i += 1)
            {
                NaNBoxing boxing = new NaNBoxing();
                boxing.SetUInt((uint)i);

                Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Uint);

                Assert.AreEqual(boxing.UIntValue, i);

            }
        }

        [TestMethod]
        public void TestBoxSbyte()
        {
            for (short i = sbyte.MinValue; i <= sbyte.MaxValue; i += 1)
            {
                NaNBoxing boxing = new NaNBoxing();
                boxing.SetSByte((sbyte)i);

                Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Sbyte);

                Assert.AreEqual(boxing.SByteValue, i);

            }
        }

        [TestMethod]
        public void TestBoxByte()
        {
            for (short i = byte.MinValue; i <= byte.MaxValue; i += 1)
            {
                NaNBoxing boxing = new NaNBoxing();
                boxing.SetByte((byte)i);

                Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Byte);

                Assert.AreEqual(boxing.ByteValue, i);

            }
        }

        [TestMethod]
        public void TestBoxShort()
        {
            for (int i = short.MinValue; i <= short.MaxValue; i += 1)
            {
                NaNBoxing boxing = new NaNBoxing();
                boxing.SetShort((short)i);

                Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Short);

                Assert.AreEqual(boxing.ShortValue, i);

            }
        }

        [TestMethod]
        public void TestBoxUShort()
        {
            for (int i = ushort.MinValue; i < ushort.MaxValue; i += 1)
            {
                NaNBoxing boxing = new NaNBoxing();
                boxing.SetUShort((ushort)i);

                Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.UShort);

                Assert.AreEqual(boxing.UShortValue, i);

            }
        }




        [TestMethod]
        public void TestBoxBoolean()
        {
            NaNBoxing boxing = new NaNBoxing();
            boxing.SetBoolean(true);
            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Boolean);
            Assert.AreEqual(boxing.Boolean,true);

            boxing.SetBoolean(false);
            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Boolean);
            Assert.AreEqual(boxing.Boolean,false);
            
        }

        [TestMethod]
        public void TestUndefined()
        {
            NaNBoxing boxing = new NaNBoxing();
            boxing.SetUndefined();

            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Undefined);

        }

        [TestMethod]
        public void TestNull()
        {
            NaNBoxing boxing = new NaNBoxing();
            boxing.SetNull();

            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Null);

        }

        [TestMethod]
        public void TestFloat()
        {
            NaNBoxing boxing = new NaNBoxing();
            boxing.SetFloat(float.Pi);
            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Float);
            Assert.AreEqual(boxing.FloatValue, float.Pi);

            boxing.SetFloat(float.NaN);
            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Float);
            Assert.AreEqual( double.IsNaN( boxing.FloatValue), true);

            boxing.SetFloat(float.PositiveInfinity);
            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Float);
            Assert.AreEqual(float.IsPositiveInfinity(boxing.FloatValue), true);

            boxing.SetFloat(float.NegativeInfinity);
            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Float);
            Assert.AreEqual(float.IsNegativeInfinity(boxing.FloatValue), true);




            boxing.SetFloat(1.0f / 0.0f);
            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Float);

            boxing.SetFloat(553.55f);
            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Float);
            Assert.AreEqual(boxing.FloatValue, 553.55f);


        }



        [TestMethod]
        public void TestNumber()
        {
            NaNBoxing boxing = new NaNBoxing();
            boxing.SetNumber( double.Pi );
            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Number);
            Assert.AreEqual(boxing.Number, double.Pi);

            boxing.SetNumber( double.NaN );
            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Number);
            Assert.AreEqual( double.IsNaN( boxing.Number), double.IsNaN( double.NaN));


            boxing.SetNumber( double.PositiveInfinity );
            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Number);
            Assert.AreEqual( double.IsPositiveInfinity( boxing.Number), double.IsPositiveInfinity( double.PositiveInfinity));

            boxing.SetNumber( double.NegativeInfinity );
            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Number);
            Assert.AreEqual( double.IsNegativeInfinity( boxing.Number), double.IsNegativeInfinity( double.NegativeInfinity));

            boxing.SetNumber(1.0 / 0.0);
            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Number);


            boxing.SetNumber(553.55);
            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.Number);
            Assert.AreEqual(boxing.Number, 553.55);

        }

		[TestMethod]
		public void TestFastComp()
        {
            NaNBoxing a = default;
            NaNBoxing b = default;

            for (int i = sbyte.MinValue; i <= sbyte.MaxValue; i++)
            {
                a.SetSByte((sbyte)i);
                b.SetInt(i);

                bool eq;
                bool success = a.FastTestComp(b, out eq);

                Assert.IsTrue(eq);
                Assert.IsTrue(success);

            }

            {

                a.SetFloat(float.NaN);
                b.SetNumber(3);

				bool eq;
				bool success = a.FastTestComp(b, out eq);

				Assert.IsFalse(eq);
				Assert.IsTrue(success);

			}

			{

				a.SetFloat(3);
				b.SetNumber(3);

				bool eq;
				bool success = a.FastTestComp(b, out eq);

				Assert.IsTrue(eq);
				Assert.IsTrue(success);

			}

			{

                a.SetHeapPtr(0);
				b.SetNumber(3);

				bool eq;
				bool success = a.FastTestComp(b, out eq);

				Assert.IsFalse(eq);
				Assert.IsFalse(success);

			}

			for (int i = ushort.MinValue; i <= ushort.MaxValue; i++)
			{
				a.SetUShort((ushort)i);
				b.SetInt(i);
				bool eq;
				bool success = a.FastTestComp(b, out eq);
				Assert.IsTrue(eq);
				Assert.IsTrue(success);


				a.SetUShort((ushort)i);
				b.SetInt(-i-1);
				
				success = a.FastTestComp(b, out eq);
				Assert.IsFalse(eq);
				Assert.IsTrue(success);

			}

		}


    }
}
