using NUnit.Framework;

namespace ShiftMagicSpell.Tests
{
    /// <summary>빌드 설정 회귀 테스트(에디터에서만 의미 있음).</summary>
    public class BuildSettingsTests
    {
        // WebGL 빌드 확인에서 발견: Unity 스플래시가 떠 있는 동안 한 첫 클릭이 무시되어 시작이 안 됐다
        [Test]
        public void SplashScreen_IsOff()
        {
#if UNITY_EDITOR
            Assert.IsFalse(UnityEditor.PlayerSettings.SplashScreen.show, "스플래시가 켜져 있으면 첫 클릭이 버려진다");
#else
            Assert.Ignore("에디터 전용");
#endif
        }
    }
}
