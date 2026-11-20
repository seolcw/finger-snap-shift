-- 에셋 전체 생성. 실행:
--   Aseprite.exe -b --script-param root=<저장소 경로> --script ArtSource/Scripts/build_assets.lua
-- 결과는 Assets/Art/ 아래 .aseprite, 팔레트는 ArtSource/Palette/ 에 저장된다.
-- 여기서 다시 만들면 Aseprite에서 손으로 고친 내용은 덮어쓴다.

local root = app.params.root
local L = dofile(root .. "/ArtSource/Scripts/fss_lib.lua")
local art = root .. "/Assets/Art/"

L.writeGpl(root .. "/ArtSource/Palette/FingerSnapShift.gpl")

-- 같은 이름이 이어지는 프레임을 한 태그로 묶어 저장한다.
local function saveTagged(path, w, h, list)
  local frames, tags = {}, {}
  for i, f in ipairs(list) do
    frames[i] = { draw = f.draw, ms = f.ms or 100 }
  end
  local i = 1
  while i <= #list do
    local j = i
    while list[j + 1] and list[j + 1].name == list[i].name do j = j + 1 end
    tags[#tags + 1] = { list[i].name, i, j }
    i = j + 1
  end
  L.buildSprite(path, w, h, frames, tags)
end

-- 마스크를 좌표로 찍어서 만들 때 쓰는 격자 (좌표는 0부터)
local function grid(w, h)
  local g = {}
  for y = 1, h do
    g[y] = {}
    for x = 1, w do g[y][x] = "." end
  end
  return g
end

local function set(g, x, y, ch)
  if g[y + 1] and g[y + 1][x + 1] then g[y + 1][x + 1] = ch end
end

local function rowsOf(g)
  local rows = {}
  for y = 1, #g do rows[y] = table.concat(g[y]) end
  return rows
end

local CLEAR = app.pixelColor.rgba(0, 0, 0, 0)

----------------------------------------------------------------------
-- 키보드 칸 (16x20: 윗면 16 + 두께 면)
----------------------------------------------------------------------
local CRACK = {
  { 5, 1 }, { 5, 2 }, { 6, 3 }, { 6, 4 }, { 7, 5 }, { 8, 6 }, { 8, 7 }, { 9, 8 }, { 9, 9 },
  { 10, 10 }, { 10, 11 }, { 9, 12 }, { 9, 13 }, { 10, 14 }, { 10, 15 }, { 10, 16 }, { 10, 17 },
  { 10, 8 }, { 11, 7 }, { 12, 7 }, { 13, 6 },
  { 8, 12 }, { 7, 13 }, { 6, 13 },
}

-- 부서진 키: 세 갈래로 갈라진 금
local SHATTER = {
  { 7, 10 }, { 6, 9 }, { 6, 8 }, { 5, 7 }, { 4, 6 }, { 4, 5 }, { 3, 4 }, { 3, 3 },
  { 7, 11 }, { 8, 12 }, { 8, 13 }, { 7, 14 }, { 7, 15 }, { 8, 16 }, { 8, 17 }, { 8, 18 },
  { 8, 10 }, { 9, 9 }, { 10, 9 }, { 11, 10 }, { 12, 10 }, { 13, 11 }, { 14, 11 },
  { 6, 10 }, { 8, 11 }, { 9, 10 }, { 5, 8 }, { 7, 13 },
}

local SHIFT_ARROW = {
  "....b....",
  "...b.b...",
  "..b...b..",
  ".b.....b.",
  "bbb...bbb",
  "..b...b..",
  "..b...b..",
  "..b...b..",
  "..bbbbb..",
}

local keyFrames = {
  { name = "Base", draw = function(img) L.drawKey(img, {}) end },
  { name = "Pressed", draw = function(img) L.drawKey(img, { press = 2 }) end },
  { name = "Cracked", draw = function(img)
    L.drawKey(img, {})
    for _, p in ipairs(CRACK) do L.px(img, p[1], p[2], "K") end
  end },
  { name = "Broken", draw = function(img)
    -- 내려앉고, 갈라지고, 오른쪽 위 모서리가 떨어져 나간 모양
    L.drawKey(img, { press = 2 })
    for _, p in ipairs(SHATTER) do L.px(img, p[1], p[2], "K") end
    for y = 2, 8 do
      local edge = 8 + (y - 2)
      for x = edge + 1, 15 do img:drawPixel(x, y, CLEAR) end
      if y > 2 then L.px(img, edge, y, "K") end
    end
    img:drawPixel(8, 2, L.C.K)
  end },
  { name = "Blank", draw = function(img)
    L.drawKey(img, { top = "D", hi = "M", side = "D", seam = true })
  end },
  { name = "Shift", draw = function(img)
    L.drawKey(img, { top = "B", hi = "W", side = "b" })
    L.drawMap(img, SHIFT_ARROW, 4, 4)
  end },
  { name = "Hole", draw = function(img)
    -- 키가 빠진 자리. 테두리는 판, 안쪽 오른쪽·아래 벽이 빛을 받는다.
    L.rect(img, 1, 4, 14, 19, "K")
    L.rect(img, 1, 4, 14, 4, "D")
    L.rect(img, 1, 19, 14, 19, "D")
    L.rect(img, 0, 5, 0, 18, "D")
    L.rect(img, 15, 5, 15, 18, "D")
    L.rect(img, 14, 6, 14, 18, "M")
    L.rect(img, 2, 18, 14, 18, "M")
  end },
}

saveTagged(art .. "Tiles/Keycap.aseprite", L.TILE_W, L.TILE_H, keyFrames)

-- 가시: 키 위에 겹쳐 그리는 오버레이. 1장은 끝만 나온 모습, 2장은 다 솟은 모습.
local function spikeMask(cx, baseY, h)
  local g = grid(L.TILE_W, L.TILE_H)
  for i = 0, h - 1 do
    local y = baseY - h + 1 + i
    local half = math.min(2, math.floor(i * 3 / h))
    for x = cx - half, cx + half do
      set(g, x, y, x <= cx and "w" or "l")
    end
  end
  return rowsOf(g)
end

local function spikes(h)
  return function(img)
    for _, s in ipairs({ { 4, 8 }, { 11, 8 }, { 4, 15 }, { 11, 15 } }) do
      L.drawMask(img, spikeMask(s[1], s[2], h), 0, 0, "R", "r")
    end
  end
end

saveTagged(art .. "Tiles/KeySpike.aseprite", L.TILE_W, L.TILE_H, {
  { name = "Spike", ms = 120, draw = spikes(3) },
  { name = "Spike", ms = 120, draw = spikes(7) },
})

-- 예고 테두리: 키 전체를 두르는 2px 테. 빨강과 어두운 빨강이 번갈아 나온다.
local function ring(ch)
  return function(img)
    L.rect(img, 0, 0, 15, 1, ch)
    L.rect(img, 0, 18, 15, 19, ch)
    L.rect(img, 0, 0, 1, 19, ch)
    L.rect(img, 14, 0, 15, 19, ch)
  end
end

saveTagged(art .. "Tiles/KeyWarning.aseprite", L.TILE_W, L.TILE_H, {
  { name = "Blink", ms = 120, draw = ring("R") },
  { name = "Blink", ms = 120, draw = ring("r") },
})

-- 목표 글자 빛남: 윗면 안쪽을 두르는 노란 테.
saveTagged(art .. "Tiles/KeyTargetGlow.aseprite", L.TILE_W, L.TILE_H, {
  { name = "Glow", draw = function(img)
    L.rect(img, 1, 1, 14, 1, "Y")
    L.rect(img, 1, 15, 14, 15, "Y")
    L.rect(img, 1, 1, 1, 15, "Y")
    L.rect(img, 14, 1, 14, 15, "Y")
  end },
})

----------------------------------------------------------------------
-- 손 = 마우스 커서 (32x32). 흰 몸통 + 밝은 회색 음영 한 단계.
----------------------------------------------------------------------
local HAND_IDLE = {
  "", "",
  "...........##",
  "..........####",
  "..........####",
  "..........####",
  "..........####",
  "..........####",
  "..........####",
  "..........####",
  "..........####",
  "..........####k####",
  "..........####k####k####",
  "..........####k####k####",
  "..........####k####k####k###",
  ".......##k####k####k####k###",
  "......###k####k####k####k###",
  ".....#######################",
  ".....#######################",
  ".....#######################",
  ".....#######################",
  "......######################",
  "........####################",
  "..........##################",
  "..........##################",
  "..........##################",
  "...........################",
  "...........################",
}

-- 엄지와 중지가 만나 고리를 만든 모습. 고리 안의 구멍이 실루엣으로 읽힌다.
local HAND_FLICK_CHARGE = {
  "", "", "",
  "....................##",
  "...............##..####",
  "..............####.####",
  "..............####.####..#",
  "..............####.####.###",
  "..............####.####.###",
  "..............####.####.###",
  ".........####k####.####.###",
  ".......######k####k####k###",
  "......#######k####k####k###",
  ".....########k#############",
  ".....###.....##############",
  ".....###.....##############",
  ".....#kk.....##############",
  ".....###.....##############",
  ".....####....##############",
  "......#####################",
  ".......####################",
  ".........##################",
  ".............##############",
  ".............##############",
  ".............##############",
  "..............############",
  "..............############",
}

local HAND_FLICK_RELEASE = {
  "............L",
  "............L......L",
  "............L......L",
  "............L......L",
  "...............##..L",
  "..............####",
  "..............####...##",
  ".........##...####..####",
  "........####..####..####",
  "........####..####..####",
  "........####..####..####..#",
  "........####..####..####.###",
  "..##....####..####..####.###",
  "..###...####..####..####.###",
  "..####..####..####..####.###",
  "...####.####..####..####.###",
  "....########################",
  ".....#######################",
  "......######################",
  "........####################",
  "........####################",
  "........####################",
  "........####################",
  "........####################",
  "........####################",
  ".........##################",
  "..........################",
}

-- 편 손에서 가리키는 손으로 돌아가는 중간: 검지만 길고 나머지는 반쯤 접힘.
local HAND_FLICK_RECOVER = {
  "", "", "", "",
  "...........##",
  "..........####",
  "..........####",
  "..........####.##",
  "..........####k####..##",
  "..........####k####k####",
  "..........####k####k####",
  "..........####k####k####..#",
  "..........####k####k####k###",
  ".....##...####k####k####k###",
  ".....###..####k####k####k###",
  ".....####k####k####k####k###",
  "......###k####k####k####k###",
  "......######################",
  "......######################",
  ".......#####################",
  ".......#####################",
  "........####################",
  ".........###################",
  "..........##################",
  "..........##################",
  "..........##################",
  "...........################",
  "...........################",
}

-- 손바닥을 펴서 들어 올림: 손가락을 붙여 곧게 세운 큰 손.
local HAND_SLAM_RAISE = {
  "",
  "..............##",
  ".............####..##",
  ".........##..####.####",
  "........####k####k####",
  "........####k####k####..#",
  "........####k####k####k###",
  "........####k####k####k###",
  "........####k####k####k###",
  "........####k####k####k###",
  "........####k####k####k###",
  "........####k####k####k###",
  "........####k####k####k###",
  "........####k####k####k###",
  "..##....####k####k####k###",
  "..###...####k####k####k###",
  "..####..##################",
  "...####.##################",
  "...#######################",
  "....######################",
  ".....#####################",
  "......####################",
  "........##################",
  "........##################",
  "........##################",
  "........##################",
  "........##################",
  ".........################",
  "..........##############",
}

-- 내려친 손: 손가락을 넓게 벌리고 납작하게 붙은 모습.
local HAND_SLAM_HIT = {
  "", "", "", "", "", "",
  ".............##",
  "............####...##",
  ".......##...####..####",
  "......####..####..####",
  "......####..####..####...#",
  "......####..####..####..###",
  "......####..####..####..###",
  "......####..####..####..###",
  "......####..####..####..###",
  "......####..####..####..###",
  "......#####################",
  "......#####################",
  ".##########################",
  ".##########################",
  "..#########################",
  "......#####################",
  "......#####################",
  "......#####################",
  ".......###################",
  "........#################",
}

-- 주먹(지팡이를 쥔 손). 엄지가 손가락 위를 가로지른다.
local HAND_FIST = {
  "...##...##...##...#",
  "..####k####k####k###",
  "..####k####k####k###",
  "..####k####k####k###",
  "..####k####k####k###",
  "..kkkkkkkkk#####k###",
  "##########k#####k###",
  "##########k#########",
  "##########k#########",
  ".kkkkkkkkk##########",
  "..##################",
  "..##################",
  "...################",
  "...################",
}

-- 맞아서 움찔: 손가락이 들쭉날쭉 움츠러들고 손 전체가 뒤로 기운다.
local HAND_HURT_BASE = {
  "", "", "", "", "", "", "", "",
  "...............##",
  ".........##...####",
  "........####..####...##",
  "........####..####..####",
  "........####..####..####..#",
  "..##....####..####..####.###",
  "..###...####..####..####.###",
  "..####..####..####..####.###",
  "...####.####..####..####.###",
  "....########################",
  ".....#######################",
  "......######################",
  "........####################",
  "........####################",
  "........####################",
  "........####################",
  "........####################",
  "........####################",
  ".........##################",
  "..........################",
}

local function shear(rows, shiftOf)
  local out = {}
  for i, row in ipairs(rows) do
    out[i] = row == "" and "" or (string.rep(".", shiftOf(i - 1)) .. row)
  end
  return out
end

local HAND_HURT = shear(HAND_HURT_BASE, function(y) return math.floor((27 - y) / 6) end)

-- 누른 순간: 검지가 짧아지고(4px) 손이 내려간다.
local HAND_CLICK_DOWN = { "", "", "", "" }
for i, row in ipairs(HAND_IDLE) do
  if i < 5 or i > 8 then HAND_CLICK_DOWN[#HAND_CLICK_DOWN + 1] = row end
end

local function hand(mask, dx, dy)
  return function(img) L.drawMask(img, mask, dx or 0, dy or 0, "W", "L") end
end

-- 지팡이: 밝은 회색 막대 + 빨간 구슬. 손보다 먼저 그려서 주먹이 위에 온다.
local function wandFrame(fistX, fistY, stickPixels, orbX, orbY)
  return function(img)
    local stick = grid(32, 32)
    for _, p in ipairs(stickPixels) do set(stick, p[1], p[2], "w") end
    L.drawMask(img, rowsOf(stick), 0, 0, "L", "M")
    local orb = grid(32, 32)
    for y = -2, 2 do
      local half = (y == -2 or y == 2) and 1 or 2
      for x = -half, half do set(orb, orbX + x, orbY + y, "#") end
    end
    L.drawMask(img, rowsOf(orb), 0, 0, "R", "r")
    L.drawMask(img, HAND_FIST, fistX, fistY, "W", "L")
  end
end

-- 든 모습은 오른쪽 위로, 휘두른 모습은 오른쪽 아래로 뻗는다.
local stickUp, stickSwing = {}, {}
for i = 0, 13 do
  stickUp[#stickUp + 1] = { 13 + i, 20 - i }
  stickUp[#stickUp + 1] = { 14 + i, 20 - i }
end
for i = 0, 11 do
  stickSwing[#stickSwing + 1] = { 12 + i, 14 + i }
  stickSwing[#stickSwing + 1] = { 13 + i, 14 + i }
end

local handFrames = {
  { name = "Idle", ms = 400, draw = hand(HAND_IDLE, 0, 1) },
  { name = "Idle", ms = 400, draw = hand(HAND_IDLE, 0, 0) },
  { name = "FlickCharge", ms = 60, draw = hand(HAND_FLICK_CHARGE, 0, 0) },
  { name = "FlickCharge", ms = 60, draw = hand(HAND_FLICK_CHARGE, 1, 0) },
  { name = "FlickRelease", ms = 120, draw = hand(HAND_FLICK_RELEASE, 0, 0) },
  { name = "FlickRecover", ms = 120, draw = hand(HAND_FLICK_RECOVER, 0, 0) },
  { name = "SlamRaise", ms = 200, draw = hand(HAND_SLAM_RAISE, 2, 0) },
  { name = "SlamHit", ms = 150, draw = hand(HAND_SLAM_HIT, 2, 4) },
  { name = "Click", ms = 150, draw = hand(HAND_IDLE, 0, -1) },
  { name = "Click", ms = 150, draw = hand(HAND_CLICK_DOWN, 0, 2) },
  { name = "Wand", ms = 200, draw = wandFrame(2, 13, stickUp, 28, 4) },
  { name = "Wand", ms = 200, draw = wandFrame(1, 6, stickSwing, 26, 27) },
  { name = "Hurt", ms = 200, draw = hand(HAND_HURT, 0, 1) },
}

saveTagged(art .. "Hand/Hand.aseprite", 32, 32, handFrames)

----------------------------------------------------------------------
-- 플레이어 (16x16 칸 안에 12px). 손(흰색)과 구별되는 분홍.
----------------------------------------------------------------------
local PLAYER_IDLE_A = {
  "", "", "", "",
  "......####",
  ".....######",
  "....########",
  "....##k##k##",
  "....##k##k##",
  "....########",
  "....########",
  "....########",
  ".....######",
  ".....##..##",
}
local PLAYER_IDLE_B = {
  "", "", "", "", "",
  ".....######",
  "....########",
  "....########",
  "....##k##k##",
  "....##k##k##",
  "....########",
  "....########",
  ".....######",
  ".....##..##",
}
-- 맞은 모습: 눈이 > < 가 되고 몸이 옆으로 밀리며 발이 벌어진다.
local PLAYER_HURT = {
  "", "", "", "",
  ".....####",
  "....######",
  "...########",
  "...##k###k#",
  "...###k#k##",
  "...##k###k#",
  "...########",
  "...########",
  "....######",
  "...##....##",
}

local function player(mask)
  return function(img) L.drawMask(img, mask, 0, 0, "P", "r") end
end

saveTagged(art .. "Player/Player.aseprite", 16, 16, {
  { name = "Idle", ms = 400, draw = player(PLAYER_IDLE_A) },
  { name = "Idle", ms = 400, draw = player(PLAYER_IDLE_B) },
  { name = "Hurt", ms = 200, draw = player(PLAYER_HURT) },
})

----------------------------------------------------------------------
-- 소환 탄 (8x8, 2프레임). 빨간 구슬, 흰 심이 커졌다 작아진다.
----------------------------------------------------------------------
local ORB = {
  "",
  "...##",
  "..####",
  ".######",
  ".######",
  "..####",
  "...##",
}

local function bullet(core)
  return function(img)
    L.drawMask(img, ORB, 0, 0, "R", "r")
    for _, p in ipairs(core) do L.px(img, p[1], p[2], "W") end
  end
end

saveTagged(art .. "Fx/SummonBullet.aseprite", 8, 8, {
  { name = "Spin", ms = 100, draw = bullet({ { 3, 3 }, { 4, 3 }, { 3, 4 }, { 4, 4 } }) },
  { name = "Spin", ms = 100, draw = bullet({
    { 3, 3 }, { 4, 3 }, { 3, 4 }, { 4, 4 },
    { 3, 2 }, { 4, 2 }, { 2, 3 }, { 2, 4 }, { 5, 3 }, { 5, 4 }, { 3, 5 }, { 4, 5 },
  }) },
})

----------------------------------------------------------------------
-- HP 칸 (8x8): 찬 칸은 플레이어 색, 빈 칸은 회색 테만.
----------------------------------------------------------------------
local HP_FULL = {
  "",
  "..####",
  ".######",
  ".######",
  ".######",
  ".######",
  "..####",
}
local HP_EMPTY = {
  "",
  "..llll",
  ".lkkkkl",
  ".lkkkkl",
  ".lkkkkl",
  ".lkkkkl",
  "..llll",
}

saveTagged(art .. "Ui/HpCell.aseprite", 8, 8, {
  { name = "Full", draw = function(img) L.drawMask(img, HP_FULL, 0, 0, "P", "r") end },
  { name = "Empty", draw = function(img) L.drawMask(img, HP_EMPTY, 0, 0, "M", "M") end },
})

----------------------------------------------------------------------
-- 글꼴 (5x7 대문자). 글자별 한 프레임, 프레임 번호 = 알파벳 순서(A=1 ... Z=26).
-- 흰색 한 가지로만 그린다. 색은 Unity에서 입힌다.
----------------------------------------------------------------------
local letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
local fontFrames = {}
for i = 1, #letters do
  local ch = letters:sub(i, i)
  fontFrames[i] = { draw = function(img) L.drawGlyph(img, ch, 0, 0, "W") end, ms = 100 }
end
L.buildSprite(art .. "Font/PixelFont.aseprite", 5, 7, fontFrames, {})

print("FSS build done")
