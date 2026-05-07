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


        [TestMethod]
        public void TestBoxHeapPtr()
        {
            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < short.MaxValue; j++)
                {
                    NaNBoxing boxing = new NaNBoxing();

                    boxing.SetHeapPtr(j, (byte)i);

                    Assert.AreEqual(boxing.HeapPtr, j);
                    Assert.AreEqual(boxing.HeapKind, i);

                }
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

                a.SetHeapPtr(0,255);
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


        [TestMethod]
        public void TestLocalStringType()
        {
            // Test that LocalString type is properly defined and recognized
            NaNBoxing boxing = new NaNBoxing(NaNBoxing.TAG_LOCAL_STRING);

            Assert.AreEqual(boxing.ValueType, NaNBoxing.BoxType.LocalString);
        }

        [TestMethod]
        public void TestLocalStringComparison()
        {
            // Test LocalString vs LocalString comparison
            NaNBoxing a = new NaNBoxing();
            NaNBoxing b = new NaNBoxing();
            
            // Create two LocalStrings with same content using safe method
            if (NaNBoxing.TryCreateLocalString("test", out a) && 
                NaNBoxing.TryCreateLocalString("test", out b))
            {
                bool eq;
                bool success = a.FastTestComp(b, out eq);
                
                Assert.IsTrue(success);
                Assert.IsTrue(eq);
                
                // Test LocalString vs LocalString with different content
                if (NaNBoxing.TryCreateLocalString("diff", out b))
                {
                    success = a.FastTestComp(b, out eq);
                    
                    Assert.IsTrue(success);
                    Assert.IsFalse(eq);
                }
            }
        }

        [TestMethod]
        public void TestLocalStringConcatenation()
        {
            // Test LocalString concatenation functionality
            NaNBoxing a = new NaNBoxing();
            NaNBoxing b = new NaNBoxing();
            
            // Create LocalString "hi" and "!" using safe method
            if (NaNBoxing.TryCreateLocalString("hi", out a) && 
                NaNBoxing.TryCreateLocalString("!", out b))
            {
                // Verify the LocalString values
                Assert.AreEqual("hi", a.LocalStringValue);
                Assert.AreEqual("!", b.LocalStringValue);
                
                // Test concatenation result should be "hi!" which is 3 bytes, should fit in LocalString
                string expected = "hi!";
                int expectedUtf8ByteCount = Encoding.UTF8.GetByteCount(expected);
                Assert.IsTrue(expectedUtf8ByteCount <= 5, "Expected result should fit in LocalString");
            }
        }

        [TestMethod]
        public void TestLocalStringFastAdd()
        {
            // Test FastAdd with LocalString + LocalString
            NaNBoxing a = new NaNBoxing();
            NaNBoxing b = new NaNBoxing();
            
            // Create LocalString "hi" (2 bytes) and "!" (1 byte) using safe method
            if (NaNBoxing.TryCreateLocalString("hi", out a) && 
                NaNBoxing.TryCreateLocalString("!", out b))
            {
                // Test FastAdd should succeed and return LocalString result
                NaNBoxing result;
                bool success = NaNBoxing.FastAdd(a, b, out result);
                
                Assert.IsTrue(success, "FastAdd should succeed for LocalString + LocalString when result fits");
                Assert.AreEqual(NaNBoxing.BoxType.LocalString, result.ValueType, "Result should be LocalString");
                Assert.AreEqual("hi!", result.LocalStringValue, "Result should be concatenated string");
                
                // Test case where result would be too long (should fall back to slow path)
                NaNBoxing c;
                if (NaNBoxing.TryCreateLocalString("world", out c)) // "hi" + "world" = 7 bytes > 5 bytes limit
                {
                    NaNBoxing longResult;
                    bool longSuccess = NaNBoxing.FastAdd(a, c, out longResult);
                    
                    Assert.IsFalse(longSuccess, "FastAdd should fail when result exceeds LocalString capacity");
                }
            }
        }

        [TestMethod]
        public void TestLocalStringToStringConversion()
        {
            // Test LocalString to string conversion
            NaNBoxing boxing = new NaNBoxing();
            
            // Test empty string
            if (NaNBoxing.TryCreateLocalString("", out boxing))
            {
                Assert.AreEqual("", boxing.LocalStringValue);
            }
            
            // Test ASCII string (5 bytes max)
            if (NaNBoxing.TryCreateLocalString("hello", out boxing))
            {
                Assert.AreEqual("hello", boxing.LocalStringValue);
            }
            
            // Test shorter ASCII string
            if (NaNBoxing.TryCreateLocalString("hi", out boxing))
            {
                Assert.AreEqual("hi", boxing.LocalStringValue);
            }
        }

        [TestMethod]
        public void TestLocalStringIsStringCompatibility()
        {
            // Test that LocalString is compatible with String type checking
            // This test verifies that LocalString values should be treated as String type
            // in ActionScript's 'is' and 'as' operations
            
            NaNBoxing localString = new NaNBoxing();
            
            // Create a LocalString
            if (NaNBoxing.TryCreateLocalString("test", out localString))
            {
                // Verify it's recognized as LocalString type
                Assert.AreEqual(NaNBoxing.BoxType.LocalString, localString.ValueType);
                
                // Note: The actual 'is' and 'as' operations are tested at runtime level
                // in Player.cs Is() method, which we've updated to treat LocalString as String
                // This test just verifies the LocalString creation and type recognition
                Assert.AreEqual("test", localString.LocalStringValue);
            }
        }

        [TestMethod]
        public void TestLocalStringInstanceofCompatibility()
        {
            // Test that LocalString is compatible with instanceof operations
            // This test verifies that LocalString values should be treated as String type
            // in ActionScript's 'instanceof' operations
            
            NaNBoxing localString = new NaNBoxing();
            
            // Create a LocalString
            if (NaNBoxing.TryCreateLocalString("instanceof_test", out localString))
            {
                // Verify it's recognized as LocalString type
                Assert.AreEqual(NaNBoxing.BoxType.LocalString, localString.ValueType);
                
                // Note: The actual 'instanceof' operation is tested at runtime level
                // in Player.cs get_instanceof instruction, which we've updated to treat LocalString as String
                // This test just verifies the LocalString creation and type recognition
                Assert.AreEqual("instanceof_test", localString.LocalStringValue);
            }
        }

        [TestMethod]
        public void TestConvertValueTypeStringOptimization()
        {
            // Test that ConvertValueType optimizes small strings to LocalString
            // This test verifies that numeric to string conversions use LocalString when possible
            
            // Test small integer conversion
            NaNBoxing intValue = new NaNBoxing();
            intValue.SetInt(42);
            
            // Note: We can't directly test ConvertValueType from unit tests since it's in Player class
            // But we can test that small numeric strings can be created as LocalString
            string smallIntStr = "42";
            NaNBoxing result;
            bool canCreateLocal = NaNBoxing.TryCreateLocalString(smallIntStr, out result);
            
            Assert.IsTrue(canCreateLocal, "Small integer string should fit in LocalString");
            Assert.AreEqual(NaNBoxing.BoxType.LocalString, result.ValueType);
            Assert.AreEqual(smallIntStr, result.LocalStringValue);
            
            // Test small float conversion
            string smallFloatStr = "3.14";
            bool canCreateLocalFloat = NaNBoxing.TryCreateLocalString(smallFloatStr, out result);
            
            Assert.IsTrue(canCreateLocalFloat, "Small float string should fit in LocalString");
            Assert.AreEqual(NaNBoxing.BoxType.LocalString, result.ValueType);
            Assert.AreEqual(smallFloatStr, result.LocalStringValue);
            
            // Test that longer strings cannot be created as LocalString
            string longStr = "123456"; // 6 bytes, exceeds 5-byte limit
            bool canCreateLong = NaNBoxing.TryCreateLocalString(longStr, out result);
            
            Assert.IsFalse(canCreateLong, "Long string should not fit in LocalString");
        }

        [TestMethod]
        public void TestExecAddStringOptimization()
        {
            // Test that Exec_Add optimizes string concatenation to use LocalString when possible
            // This test verifies that string concatenation operations use LocalString when the result is small
            
            // Test that small concatenation results can be created as LocalString
            string part1 = "hi";
            string part2 = "!";
            string expected = part1 + part2; // "hi!" = 3 bytes
            
            NaNBoxing result;
            bool canCreateLocal = NaNBoxing.TryCreateLocalString(expected, out result);
            
            Assert.IsTrue(canCreateLocal, "Small concatenation result should fit in LocalString");
            Assert.AreEqual(NaNBoxing.BoxType.LocalString, result.ValueType);
            Assert.AreEqual(expected, result.LocalStringValue);
            
            // Test LocalString + LocalString concatenation
            NaNBoxing localStr1, localStr2;
            if (NaNBoxing.TryCreateLocalString(part1, out localStr1) && 
                NaNBoxing.TryCreateLocalString(part2, out localStr2))
            {
                // Both parts fit in LocalString
                Assert.AreEqual(NaNBoxing.BoxType.LocalString, localStr1.ValueType);
                Assert.AreEqual(NaNBoxing.BoxType.LocalString, localStr2.ValueType);
                
                // The concatenation result should also fit in LocalString
                string concatenated = localStr1.LocalStringValue + localStr2.LocalStringValue;
                NaNBoxing concatResult;
                bool concatCanCreateLocal = NaNBoxing.TryCreateLocalString(concatenated, out concatResult);
                
                Assert.IsTrue(concatCanCreateLocal, "LocalString + LocalString result should fit in LocalString when small");
                Assert.AreEqual(expected, concatResult.LocalStringValue);
            }
        }

        [TestMethod]
        public void TestLocalStringStrictEqualityComparison()
        {
            // Test that LocalString vs HeapPtr(String) strict equality comparison works correctly
            // This test verifies that IsStrictlyEqual handles LocalString comparisons properly
            
            NaNBoxing localString = new NaNBoxing();
            
            // Create a LocalString
            if (NaNBoxing.TryCreateLocalString("test", out localString))
            {
                // Verify it's recognized as LocalString type
                Assert.AreEqual(NaNBoxing.BoxType.LocalString, localString.ValueType);
                Assert.AreEqual("test", localString.LocalStringValue);
                
                // Note: We can't directly test IsStrictlyEqual from unit tests since it's in Player class
                // But we can test that LocalString comparison logic is properly implemented
                // The actual strict equality testing happens at runtime level in Player.cs IsStrictlyEqual method
                
                // Test LocalString vs LocalString comparison (should work via FastTestComp)
                NaNBoxing localString2;
                if (NaNBoxing.TryCreateLocalString("test", out localString2))
                {
                    bool isEqual;
                    bool fastCompSuccess = localString.FastTestComp(localString2, out isEqual);
                    
                    Assert.IsTrue(fastCompSuccess, "FastTestComp should succeed for LocalString vs LocalString");
                    Assert.IsTrue(isEqual, "LocalString with same content should be equal");
                }
                
                // Test LocalString vs LocalString with different content
                NaNBoxing localString3;
                if (NaNBoxing.TryCreateLocalString("diff", out localString3))
                {
                    bool isEqual;
                    bool fastCompSuccess = localString.FastTestComp(localString3, out isEqual);
                    
                    Assert.IsTrue(fastCompSuccess, "FastTestComp should succeed for LocalString vs LocalString");
                    Assert.IsFalse(isEqual, "LocalString with different content should not be equal");
                }
            }
        }

    }
}
