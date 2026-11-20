-- 5차 추가 에셋 확인용 그림 두 장. 저장된 .aseprite를 다시 읽어서 그리므로 파일 자체의 확인도 된다.
--   ArtSource/Preview/asset-sheet-v5.png  : 5차에 추가한 에셋 시트 (3배)
--   ArtSource/Preview/game-preview-v5.png : 320x200 게임 화면처럼 배치한 그림 (3배)
-- 실행: Aseprite.exe -b --script-param root=<저장소 경로> --script ArtSource/Scripts/build_preview_v5.lua

local root = app.params.root
local L = dofile(root .. "/ArtSource/Scripts/fss_lib.lua")
local art = root .. "/Assets/Art/"
local P = function(p) return art .. p .. ".aseprite" end

----------------------------------------------------------------------
-- 에셋 시트 (build_preview.lua의 시트와 같은 배치 방식)
----------------------------------------------------------------------
local function assetSheet()
  local W, H = 400, 345
  local img = Image(W, H, ColorMode.RGB)
  L.rect(img, 0, 0, W - 1, H - 1, "K")
  local MARGIN = 6
  local x, y = MARGIN, MARGIN

  local function title(text) L.drawText(img, text, MARGIN, y, "W"); y = y + 11; x = MARGIN end
  local function section(groups, pad)
    local rowH = 0
    for _, g in ipairs(groups) do
      local info = L.load(g.path)
      local from, to = 1, #info.frames
      if g.tag then from, to = info.tags[g.tag][1], info.tags[g.tag][2] end
      local n = to - from + 1
      local groupW = math.max(n * (info.w + pad), #g.label * 6) + 6
      if x + groupW > W - MARGIN then x = MARGIN; y = y + rowH; rowH = 0 end
      for i = 0, n - 1 do
        local fx = x + i * (info.w + pad)
        if g.under then g.under(fx, y) end
        L.blit(img, info.frames[from + i], fx, y)
      end
      L.drawText(img, g.label:upper(), x, y + info.h + 2, "M")
      x = x + groupW
      rowH = math.max(rowH, info.h + 2 + 7 + 6)
    end
    y = y + rowH + 2; x = MARGIN
  end
  local function tags(path)
    local gs = {}
    for _, name in ipairs(L.load(path).order) do gs[#gs + 1] = { label = name, path = path, tag = name } end
    return gs
  end
  local function concat(...) local out = {}; for _, t in ipairs({ ... }) do for _, v in ipairs(t) do out[#out + 1] = v end end; return out end
  local baseUnder = function(fx, fy) L.blitTag(img, art .. "Tiles/Keycap.aseprite", "Base", 1, fx, fy) end

  title("SLOT  BLOCK")
  section(concat(tags(P("Tiles/AttackSlot")), tags(P("Tiles/AttackBlock"))), 2)
  title("KEY OVERLAY ON BASE")
  section({
    { label = "Matched", path = P("Tiles/KeyMatched"), tag = "Glow", under = baseUnder },
    { label = "Band", path = P("Tiles/KeyRowBand"), tag = "Band", under = baseUnder },
    { label = "Chevron", path = P("Fx/RotateChevron"), tag = "Chevron" },
    { label = "Edge", path = P("Fx/EdgeArrow"), tag = "Arrow" },
  }, 2)
  title("SPELL FX")
  section({
    { label = "Fireball", path = P("Fx/Fireball"), tag = "Fly" },
    { label = "Explosion", path = P("Fx/Explosion"), tag = "Burst" },
  }, 2)
  section({
    { label = "Lightning", path = P("Fx/Lightning") },
    { label = "Slash", path = P("Fx/Slash"), tag = "Swing" },
    { label = "Gust", path = P("Fx/Gust"), tag = "Spin" },
  }, 2)
  section({
    { label = "Ice", path = P("Fx/IceShard"), tag = "Shard" },
    { label = "Star", path = P("Fx/Star"), tag = "Twinkle" },
    { label = "Rocket", path = P("Fx/Rocket"), tag = "Fly" },
    { label = "Beam", path = P("Fx/Beam") },
    { label = "Shift", path = P("Fx/ShiftArrow"), tag = "Arrow" },
    { label = "Debris", path = P("Fx/BlockDebris"), tag = "Debris" },
  }, 2)
  title("SPELL ICON")
  section(tags(P("Ui/SpellIcon")), 2)
  title("UI  SYMBOLS")
  section({
    { label = "Panel", path = P("Ui/Panel"), tag = "Panel" },
    { label = "Hp", path = P("Ui/HpBar") },
    { label = "Space", path = P("Ui/SpaceKey") },
    { label = "", path = P("Font/PixelSymbols") },
  }, 2)
  L.savePngScaled(img, root .. "/ArtSource/Preview/asset-sheet-v5.png", 3)
end

----------------------------------------------------------------------
-- 게임 화면 미리보기: 왼쪽 판에서 줄을 돌리는 중, 오른쪽 슬롯에 블록 두 개, 플레이어가 슬롯에서 발동
----------------------------------------------------------------------
local function gamePreview()
  local W, H = 320, 200
  local img = Image(W, H, ColorMode.RGB)
  L.rect(img, 0, 0, W - 1, H - 1, "K")
  local KEY = art .. "Tiles/Keycap.aseprite"
  -- 왼쪽: 1행을 돌린 직후, 2행 앞에 FIRE의 F I R가 맞춰진 상태
  local LEFT = { ".IGMW_.", "_BRHNC.", "FIRDSO^", ".ZLTEPJ", ".UXQKV." }
  local RIGHT = { ".WYFV_.", ".KLOTPZ", "^CNISRU", "JDHEAQ.", "._BGMX." }
  local BX, BY = { 40, 168 }, 84

  local function board(rows, b, over)
    for r, row in ipairs(rows) do
      for c = 1, #row do
        local ch = row:sub(c, c)
        if ch ~= "." then
          local x, y = BX[b] + (c - 1) * 16, BY + (r - 1) * 20
          local tag = ch == "^" and "Shift" or ch == "_" and "Blank" or "Base"
          L.blitTag(img, KEY, tag, 1, x, y)
          local o = over(r, c)
          -- 덧그림 먼저, 글자는 맨 위에
          if o == "match" then L.blitTag(img, P("Tiles/KeyMatched"), "Glow", 1, x, y) end
          if o == "band" then L.blitTag(img, P("Tiles/KeyRowBand"), "Band", 1, x, y) end
          if ch:match("%a") then L.drawGlyph(img, ch, x + 5, y + 5, o == "match" and "G" or "W") end
        end
      end
    end
  end
  board(LEFT, 1, function(r, c) if r == 3 and c <= 3 then return "match" end; if r == 2 and c <= 6 then return "band" end end)
  board(RIGHT, 2, function() end)
  -- 1행 돌리기 화살표: 칸마다 작은 ›, 양 끝 바깥에 큰 화살표
  for c = 1, 6 do L.blitTag(img, P("Fx/RotateChevron"), "Chevron", 1, BX[1] + (c - 1) * 16 + 4, BY + 20 + 12) end
  L.blitTag(img, P("Fx/EdgeArrow"), "Arrow", 1, BX[1] + 6 * 16 + 2, BY + 20 + 4)

  -- 슬롯: 왼쪽 판 바깥 왼쪽(열 -1), 오른쪽 판 바깥 오른쪽(열 7)
  local function slot(b, r, c) return BX[b] + c * 16, BY + r * 20 end
  for _, rc in ipairs({ { 1, 1 }, { 2, 1 } }) do local x, y = slot(1, rc[1], -1); L.blitTag(img, P("Tiles/AttackSlot"), "Empty", 1, x, y) end
  local sx1, sy1 = slot(2, 1, 7); local sx2, sy2 = slot(2, 2, 7)
  L.blitTag(img, P("Tiles/AttackSlot"), "Empty", 1, sx1, sy1)
  L.blitTag(img, P("Tiles/AttackSlot"), "Active", 1, sx2, sy2)
  L.blitTag(img, P("Tiles/AttackBlock"), "Idle", 2, sx1, sy1); L.blitTag(img, P("Ui/SpellIcon"), "Bolt", 1, sx1 + 2, sy1 + 2)
  L.blitTag(img, P("Tiles/AttackBlock"), "Cracked", 1, sx2, sy2); L.blitTag(img, P("Ui/SpellIcon"), "Fire", 1, sx2 + 2, sy2 + 2)
  -- 플레이어가 아래 슬롯 위에서 Space
  L.blitTag(img, art .. "Player/Player.aseprite", "Idle", 1, sx2, sy2 - 6)
  L.blitTag(img, P("Ui/SpaceKey"), "Pressed", 1, sx2 - 4, sy2 + 22)

  -- 손과 날아가는 불덩이, 손 위 폭발
  L.blitTag(img, art .. "Hand/Hand.aseprite", "Hurt", 1, 150, 22)
  L.blitTag(img, P("Fx/Fireball"), "Fly", 2, 214, 46)
  L.blitTag(img, P("Fx/Explosion"), "Burst", 2, 150, 18)

  -- 위쪽 UI: 목표 단어 판 + 손 HP 바 + 플레이어 HP
  local PANEL = L.load(P("Ui/Panel")).frames[1]
  local function panel(x0, y0, w, h)   -- 9-slice를 손으로 흉내: 테두리 3px
    for yy = 0, h - 1 do for xx = 0, w - 1 do
      local sxp = xx < 3 and xx or (xx >= w - 3 and 16 - (w - xx) or 3 + (xx - 3) % 10)
      local syp = yy < 3 and yy or (yy >= h - 3 and 16 - (h - yy) or 3 + (yy - 3) % 10)
      local col = PANEL:getPixel(sxp, syp)
      if app.pixelColor.rgbaA(col) > 0 then img:drawPixel(x0 + xx, y0 + yy, col) end
    end end
  end
  panel(118, 4, 84, 15)
  L.blitTag(img, P("Ui/SpellIcon"), "Fire", 1, 122, 6)
  L.drawText(img, "FIRE", 138, 8, "W")
  L.drawGlyph(img, "F", 138, 8, "G"); L.drawGlyph(img, "I", 144, 8, "G"); L.drawGlyph(img, "R", 150, 8, "G")
  local SYM = L.load(P("Font/PixelSymbols")).frames
  local function sym(i, x0, y0) L.blit(img, SYM[i], x0, y0) end
  sym(4, 166, 8); sym(16, 172, 8); sym(5, 178, 8)          -- "3/4"
  for i = 0, 15 do L.blitTag(img, P("Ui/HpBar"), i < 11 and "Fill" or "Empty", 1, 246 + i * 4, 6) end
  for i = 0, 4 do L.blitTag(img, art .. "Ui/HpCell.aseprite", i < 4 and "Full" or "Empty", 1, 8 + i * 9, 6) end

  L.savePngScaled(img, root .. "/ArtSource/Preview/game-preview-v5.png", 3)
end

assetSheet()
gamePreview()
print("FSS v5 preview done")
