using ShiftMagicSpell.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftMagicSpell.Input
{
    /// <summary>
    /// 키보드를 물리 위치로 읽어(Input System의 Key는 자판 배열과 무관) GameSession에 넘긴다.
    /// WASD/방향키 이동, Shift+방향키 줄 돌리기, Space 발동, R 재시작, P 일시정지.
    /// 첫 화면은 클릭(또는 키)으로 시작한다: WebGL에서 캔버스에 키 포커스를 주고 오디오를 풀기 위해서다.
    /// 에디터·개발 빌드에서만 F1 무적, F2 페이즈 건너뛰기.
    /// </summary>
    public class GameInput
    {
        private static readonly (Key Key, MoveDir Dir)[] DirKeys =
        {
            (Key.W, MoveDir.Up),
            (Key.UpArrow, MoveDir.Up),
            (Key.S, MoveDir.Down),
            (Key.DownArrow, MoveDir.Down),
            (Key.A, MoveDir.Left),
            (Key.LeftArrow, MoveDir.Left),
            (Key.D, MoveDir.Right),
            (Key.RightArrow, MoveDir.Right),
        };

        public void Poll(GameSession game)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (game.State == GameState.Ready)
            {
                PollStart(game, keyboard);
                return;
            }

            if (keyboard.pKey.wasPressedThisFrame)
            {
                game.IsPaused = !game.IsPaused;
                game.Player.ReleaseAll();
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                game.RestartPhase();
                return;
            }

            if (Debug.isDebugBuild)
            {
                PollDebug(game, keyboard);
            }

            if (game.IsPaused)
            {
                return;
            }

            PlayerController player = game.Player;
            // 짧게 톡 친 키는 누름과 뗌이 한 프레임에 같이 들어올 수 있다(빠른 입력, 낮은 프레임). 이번 프레임에 눌렸으면 눌린 것으로 본다.
            player.IsShiftHeld = IsHeldThisFrame(keyboard.leftShiftKey) || IsHeldThisFrame(keyboard.rightShiftKey);
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                player.Fire();
            }

            foreach ((Key key, MoveDir dir) in DirKeys)
            {
                if (keyboard[key].wasPressedThisFrame)
                {
                    player.Press(dir);
                }

                // 누름과 같은 프레임에 뗐어도 놓아 줘야 계속 눌린 것으로 남지 않는다
                if (keyboard[key].wasReleasedThisFrame && !IsDirStillHeld(keyboard, dir))
                {
                    player.Release(dir);
                }
            }
        }

        // 에디터·개발 빌드 전용: F1 무적, F2 페이즈 건너뛰기
        private static void PollDebug(GameSession game, Keyboard keyboard)
        {
            if (keyboard.f1Key.wasPressedThisFrame)
            {
                game.IsInvincible = !game.IsInvincible;
                game.ShowToast(game.IsInvincible ? "DEBUG: INVINCIBLE ON" : "DEBUG: INVINCIBLE OFF");
            }

            if (keyboard.f2Key.wasPressedThisFrame && game.State == GameState.Play)
            {
                game.SkipPhase();
            }
        }

        public void OnFocusLost(GameSession game)
        {
            game.Player.ReleaseAll();
        }

        private static void PollStart(GameSession game, Keyboard keyboard)
        {
            bool isClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool isKeyPressed = keyboard.anyKey.wasPressedThisFrame && !keyboard.leftShiftKey.wasPressedThisFrame && !keyboard.rightShiftKey.wasPressedThisFrame;
            if (isClicked || isKeyPressed)
            {
                game.StartNewRun();
            }
        }

        private static bool IsHeldThisFrame(UnityEngine.InputSystem.Controls.KeyControl key)
        {
            return key.isPressed || key.wasPressedThisFrame;
        }

        // W와 ↑처럼 같은 방향 키 두 개 중 하나만 뗐으면 아직 누르고 있는 것
        private static bool IsDirStillHeld(Keyboard keyboard, MoveDir dir)
        {
            foreach ((Key key, MoveDir keyDir) in DirKeys)
            {
                if (keyDir == dir && keyboard[key].isPressed)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
