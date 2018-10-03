﻿using System;
using System.Diagnostics;
using System.Linq;
using MiningCore.Crypto.Hashing.Algorithms;
using MiningCore.Crypto.Hashing.Equihash;
using MiningCore.Crypto.Hashing.Special;
using MiningCore.Extensions;
using MiningCore.Tests.Util;
using Xunit;

namespace MiningCore.Tests.Crypto
{
    public class HashingTests : TestBase
    {
        private static readonly byte[] testValue = Enumerable.Repeat((byte) 0x80, 32).ToArray();

        // some algos need 80 byte input buffers
        private static readonly byte[] testValue2 = Enumerable.Repeat((byte)0x80, 80).ToArray();

        [Fact]
        public void Blake_Hash()
        {
            var hasher = new Blake();
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("a5adc5e82053fec28c92c31e3f17c3cfe761ddcb9435ba377671ea86a4a9e83e", result);
        }

        [Fact]
        public void Blake_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new Blake();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void Blake2s_Hash()
        {
            var hasher = new Blake2s();
            var result = hasher.Digest(testValue2).ToHexString();

            Assert.Equal("c3ee938582d70ccd9a323b6097357449365d1fdfbbe2ecd7ee44e4bdbbb24392", result);
        }

        [Fact]
        public void Blake2s_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new Blake2s();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void Groestl_Hash()
        {
            var hasher = new Groestl();
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("e14c0b9b145f2df8ebf37c81a4982a87e174a8b46c7e5ca9326d10997e02e133", result);
        }

        [Fact]
        public void Groestl_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new Groestl();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void Kezzak_Hash()
        {
            var hasher = new Kezzak();
            var result = hasher.Digest(testValue, 0ul).ToHexString();

            Assert.Equal("00b11e72b948db16a181437150237fa247f9b5932758b7d3f648832ed88e7919", result);
        }

        [Fact]
        public void Kezzak_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new Kezzak();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null, null));
        }

        [Fact]
        public void Scrypt_Hash()
        {
            var hasher = new Scrypt(1024, 1);
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("b546d334422ff5fff98e8ba847a55bbc06271c64bb5e21107b1b225f6579d40a", result);
        }

        [Fact]
        public void Scrypt_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new Scrypt(1024, 1);
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void NeoScrypt_Hash()
        {
            var hasher = new NeoScrypt(0);
            var result = hasher.Digest(testValue2).ToHexString();

            Assert.Equal("7915d56de262bf23b1fb9104cf5d2a13fcbed2f6b4b9b657309c222b09f54bc0", result);
        }

        [Fact]
        public void NeoScrypt_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new NeoScrypt(0);
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void ScryptN_Hash()
        {
            var clock = new MockMasterClock { CurrentTime = new DateTime(2017, 10, 16) };
            var hasher = new ScryptN(clock, new []{ Tuple.Create(2048L, 1389306217L) });
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("75d08b4c639645f3f1e15c7c412160867821441d365a7bbe3edf2c6b852ccb59", result);
        }

        [Fact]
        public void ScryptN_Hash_Should_Throw_On_Null_Input()
        {
            var clock = new MockMasterClock { CurrentTime = new DateTime(2017, 10, 16) };
            var hasher = new ScryptN(clock);
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void Lyra2Rev2_Hash()
        {
            var hasher = new Lyra2Rev2();
            var result = hasher.Digest(Enumerable.Repeat((byte) 5, 80).ToArray()).ToHexString();

            Assert.Equal("5cb1eea767131ab0ea446121854dffbfec1bf1f55938e9f877f9bae735a1c481", result);
        }

        [Fact]
        public void Lyra2Rev2_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new Lyra2Rev2();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void Lyra2Rev2_Hash_Should_Throw_On_Short_Input()
        {
            var hasher = new Lyra2Rev2();
            Assert.Throws<ArgumentException>(() => hasher.Digest(new byte[20]));
        }

        [Fact]
        public void Sha256D_Hash()
        {
            var hasher = new Sha256D();
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("4f4eb6dbba8198745a278997e154e8309b571259e33fce4d3a31adea39dc9173", result);
        }

        [Fact]
        public void Sha256D_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new Sha256D();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void Sha256S_Hash()
        {
            var hasher = new Sha256S();
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("bd75a82b9957d6d043076dea52262635042693f1fe23bcadadaecc908e1e5cc6", result);
        }

        [Fact]
        public void Sha256S_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new Sha256S();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void X11_Hash()
        {
            var hasher = new X11();
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("a5c7a5b1f019fab056867b53b2ca349555847082da8ec26c85066e7cb1f76559", result);
        }

        [Fact]
        public void X11_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new Sha256S();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void X17_Hash()
        {
            var hasher = new X17();
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("6a9a4f558168e60241e46fe44365021c4d7e7344144ab1739d6fb0125ac4c592", result);
        }

        [Fact]
        public void X17_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new X17();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void X16R_Hash()
        {
            var hasher = new X16R();
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("4f048b3d333cb55227ed1f596cacc614459b7820d5007c5de721994d0313fa41", result);
        }

        [Fact]
        public void X16R_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new X16R();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void X16S_Hash()
        {
            var hasher = new X16S();
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("c1b0a424e65b3e01e89de43c4007803be68164320aed1a8ab9a34924cfcc5055", result);
        }

        [Fact]
        public void X16S_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new X16S();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void Skein_Hash()
        {
            var hasher = new Skein();
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("460ed04dc407e0fd5ca4fcb1e2e93a766f5fc5991b23899d8da43f571722df27", result);
        }

        [Fact]
        public void Skein_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new Sha256S();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void Qubit_Hash()
        {
            var hasher = new Qubit();
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("93c1471bb55081ae14ffb78d18f1e1d77844013efb5b32e95269c1a9afe88a71", result);
        }

        [Fact]
        public void Qubit_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new Sha256S();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void GroestlMyriad_Hash()
        {
            var hasher = new GroestlMyriad();
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("79fd64cd7f4b9e59ea469c6dbfdfb6388c912240ab0b6065d65d21fcda3618ce", result);
        }

        [Fact]
        public void GroestlMyriad_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new Sha256S();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void DigestReverser_Hash()
        {
            var hasher = new DigestReverser(new Sha256S());
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("c65c1e8e90ccaeadadbc23fef193260435262652ea6d0743d0d657992ba875bd", result);
        }

        [Fact]
        public void DigestReverser_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new DigestReverser(new Sha256S());
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void DummyHasher_Should_Always_Throw()
        {
            var hasher = new DummyHasher();
            Assert.Throws<InvalidOperationException>(() => hasher.Digest(new byte[23]));
            Assert.Throws<InvalidOperationException>(() => hasher.Digest(null));
        }

        [Fact]
        public void EquihashVerifier_Should_Verify_Success()
        {
            var hasher = EquihashSolver.Instance.Value;
            var header = "0400000008e9694cc2120ec1b5733cc12687b609058eec4f7046a521ad1d1e3049b400003e7420ed6f40659de0305ef9b7ec037f4380ed9848bc1c015691c90aa16ff3930000000000000000000000000000000000000000000000000000000000000000c9310d5874e0001f000000000000000000000000000000010b000000000000000000000000000040".HexToByteArray();
            var solution = "00b43863a213bfe79f00337f5a729f09710abcc07035ef8ac34372abddecf2f82715f7223f075af96f0604fc124d6151fc8fb516d24a137faec123a89aa9a433f8a25a6bcfc554c28be556f6c878f96539186fab191505f278df48bf1ad2240e5bb39f372a143de1dd1b672312e00d52a3dd83f471b0239a7e8b30d4b9153027df87c8cd0b64de76749539fea376b4f39d08cf3d5e821495e52fdfa6f8085e59fc670656121c9d7c01388c8b4b4585aa7b9ac3f7ae796f9eb1fadba1730a1860eed797feabb18832b5e8f003c0adaf0788d1016e7a8969144018ecc86140aa4553962aa739a4850b509b505e158c5f9e2d5376374652e9e6d81b19fa0351be229af136efbce681463cc53d7880c1eeca3411154474ff8a7b2bac034a2026646776a517bf63921c31fbbd6be7c3ff42aab28230bfe81d33800b892b262f3579b7a41925a59f5cc1d4f523577c19ff9f92023146fa26486595bd89a1ba459eb0b5cec0578c3a071dbec73eca054c723ab30ce8e69de32e779cd2f1030e39878ac6ea3cdca743b43aedefe1a9b4f2da861038e2759defef0b8cad11d4179f2f08881b53ccc203e558c0571e049d998a257b3279016aad0d7999b609f6331a0d0f88e286a70432ca7f50a5bb8fafbbe9230b4ccb1fa57361c163d6b9f84579d61f41585a022d07dc8e55a8de4d8f87641dae777819458a2bf1bb02c438480ff11621ca8442ec2946875cce247c8877051359e9c822670d37bb00fa806e60e8e890ce62540fda2d5b1c790ca1e005030ac6d8e63db577bb98be111ee146828f9c48ee6257d7627b93ea3dd11aac3412e63dfc7ca132a73c4f51e7650f3f8ecf57bfc18716990b492d50e0a3e5fbf6136e771b91f7283ec3326209265b9531d157f8a07a4117fc8fb29ba1363afc6f9f0608251ea595256727a5bbe28f42a42edfbfa9017680e32980d4ad381612612b2bc7ad91e82eca693ea4fc27049a99636b50a576f1e55c72202d582b150ef194c1419f53177ecf315ea6b0e2f1aa8cd8f59b165aa0d89561c537fb6141f5813b7a4968fe16afc703326113f68508d88ff8d0aee1e88a84c0ae56c72f27511290ced48e93e8c95419d14aed1a5b2e9b2c9c1070c593e5eb50bb9a80e14e9f9fe501f56b1b3140159e8213b75d48d14af472a604484cd8e7e7abb6820245ed3ab29f9947463a033c586194be45eadec8392c8614d83a1e9ca0fe5655fa14f7a9c1d1f8f2185a06193ff4a3c3e9a96b02310033ceaa25894e7c56a6147e691597098054e285d39656d3d459ec5d13243c062b6eb44e19a13bdfc0b3c96bd3d1aeb75bb6b080322aea23555993cb529243958bb1a0e5d5027e6c78155437242d1d13c1d6e442a0e3783147a08bbfc0c2529fb705ad27713df40486fd58f001977f25dfd3c202451c07010a3880bca63959ca61f10ed3871f1152166fce2b52135718a8ceb239a0664a31c62defaad70be4b920dce70549c10d9138fbbad7f291c5b73fa21c3889929b143bc1576b72f70667ac11052b686891085290d871db528b5cfdc10a6d563925227609f10d1768a0e02dc7471ad424f94f737d4e7eb0fb167f1434fc4ae2d49e152f06f0845b6db0a44f0d6f5e7410420e6bd1f430b1af956005bf72b51405a04d9a5d9906ceca52c22c855785c3c3ac4c3e9bf532d31bab321e1db66f6a9f7dc9c017f2b7d8dfeb933cf5bbae71311ae318f6d187ebc5c843be342b08a9a0ff7c4b9c4b0f4fa74b13296afe84b6481440d58332e07b3d051ed55219d28e77af6612134da4431b797c63ef55bc53831e2f421db620fee51ba0967e4ed7009ef90af2204259bbfbb54537fd35c2132fa8e7f9c84bf9938d248862c6ca1cca9f48b0b33aa1589185c4eabc1c32".HexToByteArray();
            var result = hasher.Verify(header, solution);

            Assert.True(result);
        }

        [Fact]
        public void EquihashVerifier_Should_Not_Verify_Invalid_Solution()
        {
            var hasher = EquihashSolver.Instance.Value;
            var header = "0400000008e9694cc2120ec1b5733cc12687b609058eec4f7046a521ad1d1e3049b400003e7420ed6f40659de0305ef9b7ec037f4380ed9848bc1c015691c90aa16ff3930000000000000000000000000000000000000000000000000000000000000000c9310d5874e0001f000000000000000000000000000000010b000000000000000000000000000040".HexToByteArray();
            var solution = "90b43863a213bfe79f00337f5a729f09710abcc07035ef8ac34372abddecf2f82715f7223f075af96f0604fc124d6151fc8fb516d24a137faec123a89aa9a433f8a25a6bcfc554c28be556f6c878f96539186fab191505f278df48bf1ad2240e5bb39f372a143de1dd1b672312e00d52a3dd83f471b0239a7e8b30d4b9153027df87c8cd0b64de76749539fea376b4f39d08cf3d5e821495e52fdfa6f8085e59fc670656121c9d7c01388c8b4b4585aa7b9ac3f7ae796f9eb1fadba1730a1860eed797feabb18832b5e8f003c0adaf0788d1016e7a8969144018ecc86140aa4553962aa739a4850b509b505e158c5f9e2d5376374652e9e6d81b19fa0351be229af136efbce681463cc53d7880c1eeca3411154474ff8a7b2bac034a2026646776a517bf63921c31fbbd6be7c3ff42aab28230bfe81d33800b892b262f3579b7a41925a59f5cc1d4f523577c19ff9f92023146fa26486595bd89a1ba459eb0b5cec0578c3a071dbec73eca054c723ab30ce8e69de32e779cd2f1030e39878ac6ea3cdca743b43aedefe1a9b4f2da861038e2759defef0b8cad11d4179f2f08881b53ccc203e558c0571e049d998a257b3279016aad0d7999b609f6331a0d0f88e286a70432ca7f50a5bb8fafbbe9230b4ccb1fa57361c163d6b9f84579d61f41585a022d07dc8e55a8de4d8f87641dae777819458a2bf1bb02c438480ff11621ca8442ec2946875cce247c8877051359e9c822670d37bb00fa806e60e8e890ce62540fda2d5b1c790ca1e005030ac6d8e63db577bb98be111ee146828f9c48ee6257d7627b93ea3dd11aac3412e63dfc7ca132a73c4f51e7650f3f8ecf57bfc18716990b492d50e0a3e5fbf6136e771b91f7283ec3326209265b9531d157f8a07a4117fc8fb29ba1363afc6f9f0608251ea595256727a5bbe28f42a42edfbfa9017680e32980d4ad381612612b2bc7ad91e82eca693ea4fc27049a99636b50a576f1e55c72202d582b150ef194c1419f53177ecf315ea6b0e2f1aa8cd8f59b165aa0d89561c537fb6141f5813b7a4968fe16afc703326113f68508d88ff8d0aee1e88a84c0ae56c72f27511290ced48e93e8c95419d14aed1a5b2e9b2c9c1070c593e5eb50bb9a80e14e9f9fe501f56b1b3140159e8213b75d48d14af472a604484cd8e7e7abb6820245ed3ab29f9947463a033c586194be45eadec8392c8614d83a1e9ca0fe5655fa14f7a9c1d1f8f2185a06193ff4a3c3e9a96b02310033ceaa25894e7c56a6147e691597098054e285d39656d3d459ec5d13243c062b6eb44e19a13bdfc0b3c96bd3d1aeb75bb6b080322aea23555993cb529243958bb1a0e5d5027e6c78155437242d1d13c1d6e442a0e3783147a08bbfc0c2529fb705ad27713df40486fd58f001977f25dfd3c202451c07010a3880bca63959ca61f10ed3871f1152166fce2b52135718a8ceb239a0664a31c62defaad70be4b920dce70549c10d9138fbbad7f291c5b73fa21c3889929b143bc1576b72f70667ac11052b686891085290d871db528b5cfdc10a6d563925227609f10d1768a0e02dc7471ad424f94f737d4e7eb0fb167f1434fc4ae2d49e152f06f0845b6db0a44f0d6f5e7410420e6bd1f430b1af956005bf72b51405a04d9a5d9906ceca52c22c855785c3c3ac4c3e9bf532d31bab321e1db66f6a9f7dc9c017f2b7d8dfeb933cf5bbae71311ae318f6d187ebc5c843be342b08a9a0ff7c4b9c4b0f4fa74b13296afe84b6481440d58332e07b3d051ed55219d28e77af6612134da4431b797c63ef55bc53831e2f421db620fee51ba0967e4ed7009ef90af2204259bbfbb54537fd35c2132fa8e7f9c84bf9938d248862c6ca1cca9f48b0b33aa1589185c4eabc1c32".HexToByteArray();
            var result = hasher.Verify(header, solution);

            Assert.False(result);
        }

        [Fact]
        public void EquihashVerifier_Should_Not_Verify_Fake_Solution()
        {
            var hasher = EquihashSolver.Instance.Value;
            var header = "0400000008e9694cc2120ec1b5733cc12687b609058eec4f7046a521ad1d1e3049b400003e7420ed6f40659de0305ef9b7ec037f4380ed9848bc1c015691c90aa16ff3930000000000000000000000000000000000000000000000000000000000000000c9310d5874e0001f000000000000000000000000000000010b000000000000000000000000000040".HexToByteArray();
            var solution = Enumerable.Repeat((byte) 0, 1344).ToArray();
            var result = hasher.Verify(header, solution);

            Assert.False(result);
        }

        [Fact]
        public void EquihashVerifier_Should_Throw_On_Null_Input()
        {
            var hasher = EquihashSolver.Instance.Value;
            Assert.Throws<ArgumentNullException>(() => hasher.Verify(null, null));
        }

        [Fact]
        public void EquihashVerifier_Should_Throw_On_Wrong_Argument_Length()
        {
            var hasher = EquihashSolver.Instance.Value;
            Assert.Throws<ArgumentException>(() => hasher.Verify(new byte[3], null));
            Assert.Throws<ArgumentException>(() => hasher.Verify(new byte[140], new byte[3]));
        }

        [Fact]
        public void Equihash210Verifier_Should_Verify_Success() {
            var hasher = EquihashSolver.Instance.Value;
            var header = "8b6ca38e5990b049dcd39281ee236bd512f8e40bc1bd184bd2898bfa7628fb19bc4fe05100000000010000000000000000000000000000000000000000000000".HexToByteArray();
            var solution = "0007e15ff581ce21149c4b119ba719b6a48e87e369e71becbce53dd59c16b744e381885bcfd97825e273a4b3030988f3f1f797782c68881592269d108affb07c640e0ad0488819bbefbfbc8b7c860b76bb0b6a57c03a3ca302f0b8754afca9a4794ef71983210c9f29807233faee03ce831a0e73d5f5997b2f8a745ed206bb21612fde9e1ad7d5cfac32d147fb68749de3dafffa4a367bbaf6235e399eec609875fa228c2771cade47c57b7c9d781611015020cc434ae1c230e16c444666b1e13bc4bbb6fcae04869bd07a72896aa6b11475645f21ad5bfe75763f9b03476174b17255d89619d3445419aa41c6475633bad320b45975737c47cafb1aae2a56e5e3d885e9ee329ef408f6575a6350dc5933ac6930d013af77c82061284e610a9caacd6662f3f8356fdb19f8cceb91d63eccbfe262382e5a0771d47b6bd2ee7c68b7a6951d7a5549f225b991077327693a8dc33c21e79ca2c7e8c73c1dcbf8361f0510c5583c168c3d7e1f2e42a3e120fef6be14e131a310f64a8dbdc8c54be5ce9e388ca36e8da3da2a9d54680618071c6f057618aded8009d65029e65391a076c18e2e7ffe52b03a9c6677d3a35a987a16dc0eae2b7ffb120687fb3d9809384cb96a3d39518da9417977e42b9aad101da3f4549490ef7877186681425bf3c94fdd2810562bd0f0dbbeb621492765d47537fa34e9596b1fb170e97c843a5e5739118775ccf58b45924821695474b8f24c054fe2e1584332c739709139e987644db620b37ab2a843c88944ed4479fde048195612bead5997b5f1fb3b520af449d73832a33cb4e5ee5b568716eb45b8857d18192d4f1eaebaa3826b1125718a913b0f9daa5d7b2981ea05fef8c663521519f310300619f53aa0c99a33360d1109201e60a6a10ec79b5ab21bca224860360014bec6681fc1996199291f7a31e601451beec973d5a212e4f5f524beeb4ea0e4d7ff3fe49947ec2e548475ceafdb535e023eadba3114e3d2206186934a53d51c4b2a4439c6cb3db3367b962e29023a267d72f979fe119e6be2bcad6408e5e70c5fa748b82eecda89dc67963ddaec243bfc4209fbeb2395b82df8eb3027622621f34e0df6e63d811c07e3b6ab4bc52ae259154e18e1938da3891ef8b19abe35249df72828dfa1fd1d05502ff7a63a45fa26706c233c7465c3afca317d71b5143fbdad6e6905855b96f03a63ab7eb7f26899af659d6280210ab2688a6cc771328b056c68742925075cec86995e38be102fa76ca6bbdba30665ffa100062a56f74b6f0a4bf6976d843902a7cddc0dd3d3b90633950963c9e5205e22318522a62267a6b4117f12131e111ce09239a620b7b5fdaac269190c75ad0859ea0d5cd64eccb0ee0119f173727ce1bcb38a5ffe49ce59d5df3a6faf2e8c7b5db6e62ec2ddb0023c257b0a1f92fbfff1c25d4ce2602cd5a78c5622f5a75c79e10d4a0dd4f5048882b2f29c1132fd996cc3722775e9120254658ee5877081694497076235492ea47c549544322610acc1e06ed7cbbd15b4833b6669278b78d4fc31780776b5e999d09d4c3e556942a97ff383680e967c322c1ba95a1302fb631635fc9247a59b0e17557a3c350f0d07a40b44cb35e258efa063c015bb0a425c1126f1d6c40feb388d55369f2c20c4347dd2622833cb62552ee24d0feb210d8bcea98d7e40dda2da9e8d373ba488be0b881ab91654817defb6bb6e8820a434ea2372d5d33561ea056391f14f35132baa693c251b574808131588a26cb268fe4e3cae2ac14fb6e3cd7465fb4af1fa9a96b40218073d028a0a663a522d0097677fc26cfabac595b2f4800cb6437865c2a989ed216f6249ff6a99fdb7fefb27360a48b6de7ed7d00ffa366a26a72cab98f750356ccacc0aa609bdb1537e1ee03c0959f053edbc88337fe6c85f1afa8921547a41e4353737697a471087f7b834b799e021d3796e91b58a1d1fe6ac35579dc681f87b4fa4553f".HexToByteArray();
            var result = hasher.Verify210(header, solution);

            Assert.True(false);
        }

        [Fact]
        public void Sha3_256_Hash()
        {
            var hasher = new Sha3_256();
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("00b11e72b948db16a181437150237fa247f9b5932758b7d3f648832ed88e7919", result);
        }

        [Fact]
        public void Sha3_256_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new Sha3_256();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }

        [Fact]
        public void Sha3_512_Hash()
        {
            var hasher = new Sha3_512();
            var result = hasher.Digest(testValue).ToHexString();

            Assert.Equal("e0883cffc9ff0ecf41fca8ade29dba1fc0df4b15beccc06ca03283805e176e497f0dd33db3bda375b199a4bb5eb1bb3ba884f3cc26f65f7acf08e1307058cc8d", result);
        }

        [Fact]
        public void Sha3_512_Hash_Should_Throw_On_Null_Input()
        {
            var hasher = new Sha3_512();
            Assert.Throws<ArgumentNullException>(() => hasher.Digest(null));
        }
    }
}
