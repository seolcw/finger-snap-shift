-- 확인용 그림 두 장을 만든다. 저장된 .aseprite를 다시 읽어서 그리므로 파일 자체의 확인도 된다.
--   ArtSource/Preview/game-preview.png : 320x200 게임 화면처럼 배치한 그림 (3배)
--   ArtSource/Preview/asset-sheet.png  : 전체 에셋을 한눈에 보는 시트 (3배)
-- 실행: Aseprite.exe -b --script-param root=<저장소 경로> --script ArtSource/Scripts/build_preview.lua

local root = app.params.root
local L = dofile(root .. "/ArtSource/Scripts/fss_lib.lua")
local art = root .. "/Assets/Art/"

local KEY = art .. "Tiles/Keycap.aseprite"
local SPIKE = art .. "Tiles/KeySpike.aseprite"
local WARN = art .. "Tiles/KeyWarning.aseprite"
local GLOW = art .. "Tiles/KeyTargetGlow.aseprite"
local HAND = art .. "Hand/Hand.aseprite"
local PLAYER = art .. "Player/Player.aseprite"
local BULLET = art .. "Fx/SummonBullet.aseprite"
local HP = art .. "Ui/HpCell.aseprite"
local FONT = art .. "Font/PixelFont.aseprite"

----------------------------------------------------------------------
-- 게임 화면 미리보기
----------------------------------------------------------------------
local function gamePreview()
  local W, H = 320, 200
  local img = Image(W, H, ColorMode.RGB)
  L.rect(img, 0, 0, W - 1, H - 1, "K")

  local LEFT = { ".IGMW_.", "BRHNC_.", "YADSOT^", ".ZLFEPJ", ".UXQKV." }
  local RIGHT = { ".WYFV_.", ".KLOTPZ", "^CNISRU", "JDHEAQ.", "._BGMX." }

  -- 미리보기에서 보여 줄 칸 상태: [보드][행][열] = 상태
  local state = {
    L = { [3] = { [4] = "Pressed" }, [2] = { [2] = "Cracked" } },
    R = { [4] = { [3] = "Hole" }, [2] = { [5] = "Broken" }, [5] = { [4] = "Cracked" } },
  }
  local overlay = {
    L = { [4] = { [5] = "Glow" }, [1] = { [3] = "Warn1" }, [2] = { [3] = "Warn1" }, [3] = { [3] = "Warn1" },
          [5] = { [3] = "Warn1" } },
    R = { [3] = { [4] = "Spike2", [5] = "Spike2", [6] = "Spike1" } },
  }
  overlay.L[4][3] = "Warn1"

  local function board(rows, id, bx, by)
    for r, row in ipairs(rows) do
      for c = 1, #row do
        local ch = row:sub(c, c)
        if ch ~= "." then
          local x, y = bx + (c - 1) * 16, by + (r - 1) * 20
          local st = state[id] and state[id][r] and state[id][r][c]
          local tag = st or "Base"
          if ch == "^" then tag = "Shift" elseif ch == "_" then tag = "Blank" end
          L.blitTag(img, KEY, tag, 1, x, y)
          local ov = overlay[id] and overlay[id][r] and overlay[id][r][c]
          if ch:match("%a") and tag ~= "Hole" and tag ~= "Broken" then
            local down = (tag == "Pressed") and 2 or 0
            L.drawGlyph(img, ch, x + 5, y + 5 + down, ov == "Glow" and "Y" or "W")
          end
          if ov == "Glow" then L.blitTag(img, GLOW, "Glow", 1, x, y) end
          if ov == "Warn1" then L.blitTag(img, WARN, "Blink", 1, x, y) end
          if ov == "Spike1" then L.blitTag(img, SPIKE, "Spike", 1, x, y) end
          if ov == "Spike2" then L.blitTag(img, SPIKE, "Spike", 2, x, y) end
        end
      end
    end
  end

  local BY = 84
  board(LEFT, "L", 40, BY)
  board(RIGHT, "R", 168, BY)

  -- 플레이어: 왼쪽 판 3행 4열 칸을 누르고 있는 모습
  L.blitTag(img, PLAYER, "Idle", 1, 40 + 3 * 16, BY + 2 * 20 + 1)

  -- 손과 탄
  L.blitTag(img, HAND, "Wand", 2, 150, 28)
  L.blitTag(img, BULLET, "Spin", 1, 196, 62)
  L.blitTag(img, BULLET, "Spin", 2, 222, 50)
  L.blitTag(img, BULLET, "Spin", 1, 250, 66)

  -- HP: 찬 칸 3, 빈 칸 2
  for i = 0, 4 do
    L.blitTag(img, HP, i < 3 and "Full" or "Empty", 1, 40 + i * 9, 188)
  end

  L.savePngScaled(img, root .. "/ArtSource/Preview/game-preview.png", 3)
end

----------------------------------------------------------------------
-- 전체 시트: 파일별로 태그 묶음을 흘려 배치하고, 태그 이름을 아래에 적는다.
----------------------------------------------------------------------
local function assetSheet()
  local W, H = 400, 306
  local img = Image(W, H, ColorMode.RGB)
  L.rect(img, 0, 0, W - 1, H - 1, "K")

  local MARGIN = 6
  local x, y = MARGIN, MARGIN

  local function title(text)
    L.drawText(img, text, MARGIN, y, "W")
    y = y + 11
    x = MARGIN
  end

  -- groups: { { label, path, tag(nil이면 전체 프레임), under(밑에 깔 그림 함수) }, ... }
  local function section(groups, pad)
    local rowH = 0
    for _, g in ipairs(groups) do
      local info = L.load(g.path)
      local from, to = 1, #info.frames
      if g.tag then from, to = info.tags[g.tag][1], info.tags[g.tag][2] end
      local n = to - from + 1
      local framesW = n * (info.w + pad)
      local groupW = math.max(framesW, #g.label * 6) + 6
      if x + groupW > W - MARGIN then
        x = MARGIN
        y = y + rowH
        rowH = 0
      end
      for i = 0, n - 1 do
        local fx = x + i * (info.w + pad)
        if g.under then g.under(fx, y) end
        L.blit(img, info.frames[from + i], fx, y)
      end
      L.drawText(img, g.label:upper(), x, y + info.h + 2, "M")
      x = x + groupW
      rowH = math.max(rowH, info.h + 2 + 7 + 6)
    end
    y = y + rowH + 2
    x = MARGIN
  end

  local function tagGroups(path, pad)
    local groups = {}
    for _, name in ipairs(L.load(path).order) do
      groups[#groups + 1] = { label = name, path = path, tag = name }
    end
    section(groups, pad)
  end

  -- 팔레트
  title("PALETTE")
  for i, p in ipairs(L.PALETTE) do
    local px = MARGIN + (i - 1) * 14
    L.rect(img, px, y, px + 11, y + 11, "D")
    L.rect(img, px + 1, y + 1, px + 10, y + 10, p[1])
  end
  y = y + 18

  title("KEYCAP")
  tagGroups(KEY, 2)

  title("KEY OVERLAY ON BASE")
  local function baseUnder(fx, fy) L.blitTag(img, KEY, "Base", 1, fx, fy) end
  section({
    { label = "Spike", path = SPIKE, tag = "Spike", under = baseUnder },
    { label = "Blink", path = WARN, tag = "Blink", under = baseUnder },
    { label = "Glow", path = GLOW, tag = "Glow", under = baseUnder },
  }, 2)

  title("HAND")
  tagGroups(HAND, 2)

  title("PLAYER  BULLET  HP")
  section({
    { label = "Idle", path = PLAYER, tag = "Idle" },
    { label = "Hurt", path = PLAYER, tag = "Hurt" },
    { label = "Spin", path = BULLET, tag = "Spin" },
    { label = "Full", path = HP, tag = "Full" },
    { label = "Empty", path = HP, tag = "Empty" },
  }, 2)

  title("FONT")
  section({ { label = "", path = FONT } }, 2)

  L.savePngScaled(img, root .. "/ArtSource/Preview/asset-sheet.png", 3)
end

gamePreview()
assetSheet()
print("FSS preview done")
