-- 5차 규칙(줄 돌리기 · 공격 단어 마법 · 공격 슬롯)에 필요한 추가 에셋. 기존 에셋은 건드리지 않는다.
-- 실행:
--   Aseprite.exe -b --script-param root=<저장소 경로> --script ArtSource/Scripts/build_assets_v5.lua
-- 규칙은 build_assets.lua와 같다: fss_lib의 12색 팔레트, 검은 1px 외곽선, 빛은 왼쪽 위.
-- 색을 Unity에서 입히는(틴트) 그림은 흰색·밝은 회색으로만 그린다: AttackBlock, Beam, PixelSymbols.

local root = app.params.root
local L = dofile(root .. "/ArtSource/Scripts/fss_lib.lua")
local art = root .. "/Assets/Art/"

-- 같은 이름이 이어지는 프레임을 한 태그로 묶어 저장한다. (build_assets.lua와 같은 방식)
local function saveTagged(path, w, h, list)
  local frames, tags = {}, {}
  for i, f in ipairs(list) do frames[i] = { draw = f.draw, ms = f.ms or 100 } end
  local i = 1
  while i <= #list do
    local j = i
    while list[j + 1] and list[j + 1].name == list[i].name do j = j + 1 end
    tags[#tags + 1] = { list[i].name, i, j }
    i = j + 1
  end
  L.buildSprite(path, w, h, frames, tags)
end

-- 좌표 함수로 마스크를 만든다. f(x, y)가 돌려준 기호(없으면 '.')를 찍는다. 좌표는 0부터.
local function maskOf(w, h, f)
  local rows = {}
  for y = 0, h - 1 do
    local t = {}
    for x = 0, w - 1 do t[#t + 1] = f(x, y) or "." end
    rows[#rows + 1] = table.concat(t)
  end
  return rows
end

local function dist(x, y, cx, cy) return math.sqrt((x - cx) ^ 2 + (y - cy) ^ 2) end
-- 결정적인 잡음(같은 입력이면 같은 값): 폭발 가장자리를 들쭉날쭉하게
local function noise(x, y, s) return ((x * 73856093 ~ y * 19349663 ~ s * 83492791) % 997) / 997 end
local function disc(cx, cy, r, ch) return function(x, y) if dist(x, y, cx, cy) <= r then return ch end end end

----------------------------------------------------------------------
-- 공격 슬롯 (16x20, 키 칸과 같은 크기). 판 바깥의 받침.
----------------------------------------------------------------------
local function slotFrame(bracket)
  return function(img)
    -- 한 단 내려앉은 어두운 받침
    L.drawKey(img, { top = "D", hi = "D", side = "K", press = 4 })
    L.rect(img, 3, 7, 12, 17, "K")
    -- 네 모서리 괄호: 블록이 들어갈 자리
    for _, c in ipairs({ { 2, 6, 1, 1 }, { 13, 6, -1, 1 }, { 2, 18, 1, -1 }, { 13, 18, -1, -1 } }) do
      local x, y, dx, dy = c[1], c[2], c[3], c[4]
      L.px(img, x, y, bracket); L.px(img, x + dx, y, bracket); L.px(img, x + 2 * dx, y, bracket)
      L.px(img, x, y + dy, bracket); L.px(img, x, y + 2 * dy, bracket)
    end
  end
end

saveTagged(art .. "Tiles/AttackSlot.aseprite", L.TILE_W, L.TILE_H, {
  { name = "Empty", draw = slotFrame("Y") },
  { name = "Active", draw = slotFrame("W") },   -- 플레이어가 서 있을 때
})

----------------------------------------------------------------------
-- 공격 블록 (16x20). 흰색·밝은 회색으로만 그려서 Unity에서 마법 색으로 틴트한다.
-- 가운데는 비워 둔다: 마법 아이콘(Ui/SpellIcon, 12x12)을 겹쳐 그린다. 16px 칸에는 4~6글자 단어가 들어가지 않아서
-- 단어는 화면 아래 "Space: FIRE 발동" 안내로 보여 준다.
----------------------------------------------------------------------
local BLOCK_CRACK = { { 4, 2 }, { 5, 3 }, { 5, 4 }, { 6, 5 }, { 7, 6 }, { 7, 7 }, { 8, 8 }, { 8, 9 }, { 9, 10 },
  { 10, 11 }, { 10, 12 }, { 11, 13 }, { 8, 7 }, { 9, 6 }, { 10, 6 }, { 11, 5 }, { 6, 11 }, { 5, 12 }, { 4, 13 }, { 7, 10 } }

local function blockFrame(sparkle, cracked)
  return function(img)
    L.drawKey(img, { top = "W", hi = "W", side = "L" })
    -- 안쪽 테: 블록이 키보다 단단해 보이게
    L.rect(img, 2, 2, 13, 2, "L"); L.rect(img, 2, 14, 13, 14, "L")
    L.rect(img, 2, 2, 2, 14, "L"); L.rect(img, 13, 2, 13, 14, "L")
    for _, p in ipairs({ { 3, 3 }, { 12, 3 }, { 3, 13 }, { 12, 13 } }) do L.px(img, p[1], p[2], "M") end
    if sparkle then
      L.px(img, 4, 4, "W"); L.px(img, 5, 4, "L"); L.px(img, 4, 5, "L")
    end
    if cracked then for _, p in ipairs(BLOCK_CRACK) do L.px(img, p[1], p[2], "K") end end
  end
end

saveTagged(art .. "Tiles/AttackBlock.aseprite", L.TILE_W, L.TILE_H, {
  { name = "Idle", ms = 500, draw = blockFrame(false, false) },
  { name = "Idle", ms = 200, draw = blockFrame(true, false) },
  { name = "Cracked", draw = blockFrame(false, true) },
})

----------------------------------------------------------------------
-- 키 위 덧그림 (16x20)
----------------------------------------------------------------------
-- 제자리에 맞은 글자: 초록 테 (KeyTargetGlow와 같은 자리, 색만 다름)
saveTagged(art .. "Tiles/KeyMatched.aseprite", L.TILE_W, L.TILE_H, {
  { name = "Glow", draw = function(img)
    L.rect(img, 1, 1, 14, 1, "G"); L.rect(img, 1, 15, 14, 15, "G")
    L.rect(img, 1, 1, 1, 15, "G"); L.rect(img, 14, 1, 14, 15, "G")
    L.rect(img, 2, 2, 13, 2, "G"); L.rect(img, 2, 2, 2, 14, "G")
  end },
})

-- 돌릴 수 있는 줄 / 돌리는 중인 줄: 하늘색 테 + 성긴 사선. 글자를 가리지 않게 가운데는 거의 비운다.
-- 두 장을 번갈아 보여 주면 사선이 흐르는 느낌.
local function band(phase)
  return function(img)
    L.rect(img, 1, 1, 14, 1, "B"); L.rect(img, 1, 15, 14, 15, "B"); L.rect(img, 1, 1, 1, 15, "B"); L.rect(img, 14, 1, 14, 15, "B")
    for y = 2, 14 do for x = 2, 13 do
      local onEdge = x <= 3 or x >= 12 or y <= 3 or y >= 13
      if onEdge and (x + y + phase * 2) % 4 == 0 then L.px(img, x, y, "B") end
    end end
  end
end
saveTagged(art .. "Tiles/KeyRowBand.aseprite", L.TILE_W, L.TILE_H, {
  { name = "Band", ms = 120, draw = band(0) },
  { name = "Band", ms = 120, draw = band(1) },
})

----------------------------------------------------------------------
-- 줄 돌리기 화살표. 오른쪽을 가리키게 그렸다(Unity에서 90도씩 돌려 위·아래·왼쪽).
----------------------------------------------------------------------
saveTagged(art .. "Fx/RotateChevron.aseprite", 8, 8, {
  { name = "Chevron", draw = function(img)
    L.drawMask(img, { "#.....", "##....", ".##...", "..##..", ".##...", "##....", "#....." }, 2, 1, "B", "b")
  end },
})

saveTagged(art .. "Fx/EdgeArrow.aseprite", 12, 12, {
  { name = "Arrow", draw = function(img)
    L.drawMask(img, {
      "......#...", "......##..", "......###.", "#########.", "##########",
      "#########.", "......###.", "......##..", "......#...",
    }, 1, 2, "B", "b")
  end },
})

----------------------------------------------------------------------
-- 마법 연출
----------------------------------------------------------------------
-- 불덩이 16x16 (FIRE, FLAME, COMET, BLAZE, DRAGON). 오른쪽으로 날아가는 모양, 꼬리가 흔들린다.
local function fireball(f)
  return function(img)
    local wave = ({ 0, 1, -1 })[f]
    L.drawMask(img, maskOf(16, 16, function(x, y)
      if dist(x, y, 10, 8) <= 4.6 then return "#" end
      local tail = (x - 1) / 2.4
      if x >= 1 and x <= 9 and math.abs(y - 8 - wave * (9 - x) / 8) <= tail then return "#" end
    end), 0, 0, "R", "r")
    L.drawMap(img, maskOf(16, 16, disc(10.5, 7.5, 2.6, "Y")), 0, 0)
    L.px(img, 10, 6, "W"); L.px(img, 11, 7, "W")
  end
end
saveTagged(art .. "Fx/Fireball.aseprite", 16, 16, {
  { name = "Fly", ms = 80, draw = fireball(1) },
  { name = "Fly", ms = 80, draw = fireball(2) },
  { name = "Fly", ms = 80, draw = fireball(3) },
})

-- 폭발 32x32: 흰 섬광 → 노란 불꽃 → 빨간 고리 → 연기
local function explosion(f)
  return function(img)
    local cx, cy = 15.5, 15.5
    if f == 1 then
      L.drawMask(img, maskOf(32, 32, disc(cx, cy, 6, "#")), 0, 0, "Y", "Y")
      L.drawMap(img, maskOf(32, 32, disc(cx, cy, 3.5, "W")), 0, 0)
    elseif f == 2 then
      L.drawMask(img, maskOf(32, 32, function(x, y) if dist(x, y, cx, cy) <= 10 + noise(x, y, 2) * 2 then return "#" end end), 0, 0, "R", "r")
      L.drawMap(img, maskOf(32, 32, disc(cx, cy, 7, "Y")), 0, 0)
      L.drawMap(img, maskOf(32, 32, disc(cx - 1, cy - 1, 3.5, "W")), 0, 0)
    elseif f == 3 then
      L.drawMask(img, maskOf(32, 32, function(x, y)
        local d = dist(x, y, cx, cy); if d <= 13 + noise(x, y, 3) * 2 and d >= 7 + noise(x, y, 4) * 3 then return "#" end
      end), 0, 0, "R", "r")
      L.drawMap(img, maskOf(32, 32, function(x, y) local d = dist(x, y, cx, cy); if d >= 9 and d <= 10.5 and noise(x, y, 5) > 0.4 then return "Y" end end), 0, 0)
    else
      -- 연기: 회색 덩어리 몇 개
      for _, c in ipairs({ { 9, 10, 4 }, { 21, 9, 3.5 }, { 23, 20, 4.5 }, { 10, 22, 3.5 }, { 16, 15, 3 } }) do
        L.drawMask(img, maskOf(32, 32, disc(c[1], c[2], c[3], "#")), 0, 0, "M", "D")
      end
    end
  end
end
saveTagged(art .. "Fx/Explosion.aseprite", 32, 32, {
  { name = "Burst", ms = 60, draw = explosion(1) },
  { name = "Burst", ms = 80, draw = explosion(2) },
  { name = "Burst", ms = 90, draw = explosion(3) },
  { name = "Burst", ms = 140, draw = explosion(4) },
})

-- 번개 16x48 (BOLT, STORM): 위에서 아래로 내리친다. 꺾이는 점을 프레임마다 다르게.
local BOLT_PATHS = {
  { { 8, 0 }, { 5, 9 }, { 10, 16 }, { 6, 26 }, { 11, 33 }, { 7, 41 }, { 8, 47 } },
  { { 9, 0 }, { 11, 8 }, { 6, 17 }, { 10, 25 }, { 5, 34 }, { 9, 41 }, { 8, 47 } },
}
local function boltMask(path, thick, branch)
  local on = {}
  local function plot(x, y) for dx = 0, thick - 1 do on[y * 64 + x + dx] = true end end
  local function seg(a, b)
    local n = math.max(math.abs(b[1] - a[1]), math.abs(b[2] - a[2]))
    for i = 0, n do plot(math.floor(a[1] + (b[1] - a[1]) * i / n + 0.5), math.floor(a[2] + (b[2] - a[2]) * i / n + 0.5)) end
  end
  for i = 1, #path - 1 do seg(path[i], path[i + 1]) end
  if branch then seg(path[3], { path[3][1] + 4, path[3][2] + 7 }) end
  return maskOf(16, 48, function(x, y) if on[y * 64 + x] then return "#" end end)
end
saveTagged(art .. "Fx/Lightning.aseprite", 16, 48, {
  { name = "Strike", ms = 60, draw = function(img) L.drawMask(img, boltMask(BOLT_PATHS[1], 2, false), 0, 0, "W", "Y") end },
  { name = "Strike", ms = 60, draw = function(img) L.drawMask(img, boltMask(BOLT_PATHS[2], 2, true), 0, 0, "W", "Y") end },
  { name = "Fade", ms = 80, draw = function(img) L.drawMask(img, boltMask(BOLT_PATHS[1], 1, false), 0, 0, "Y", "Y") end },
})

-- 얼음 조각 8x8 (FROST): 세로 마름모. Unity에서 돌려 가며 날린다.
saveTagged(art .. "Fx/IceShard.aseprite", 8, 8, {
  { name = "Shard", draw = function(img)
    L.drawMask(img, { "..#..", ".###.", ".###.", "#####", ".###.", ".###.", "..#.." }, 2, 1, "B", "b")
    L.px(img, 3, 3, "W"); L.px(img, 3, 4, "W")
  end },
})

-- 별 8x8 (MAGIC, NOVA): 작은 별 ↔ 큰 별로 반짝인다
saveTagged(art .. "Fx/Star.aseprite", 8, 8, {
  { name = "Twinkle", ms = 120, draw = function(img)
    L.drawMask(img, { ".#.", "###", ".#." }, 3, 3, "P", "P"); L.px(img, 3, 3, "W")
  end },
  { name = "Twinkle", ms = 120, draw = function(img)
    L.drawMask(img, { "..#..", "..#..", "#####", "..#..", "..#.." }, 2, 2, "P", "P"); L.px(img, 3, 3, "W")
  end },
})

-- 칼날 베기 32x32 (BLADE): 초승달 궤적. 가늘게 → 두껍게 → 회색으로 사라짐
local function slash(f)
  local inner = ({ 12.9, 11.6, 12.6 })[f]
  return function(img)
    L.drawMask(img, maskOf(32, 32, function(x, y)
      if dist(x, y, 15, 17) <= 13 and dist(x, y, 19, 13) > inner then return "#" end
    end), 0, 0, f == 3 and "L" or "W", f == 3 and "M" or "L")
  end
end
saveTagged(art .. "Fx/Slash.aseprite", 32, 32, {
  { name = "Swing", ms = 50, draw = slash(1) },
  { name = "Swing", ms = 70, draw = slash(2) },
  { name = "Swing", ms = 90, draw = slash(3) },
})

-- 돌풍 고리 16x16 (WIND, VORTEX): 빈틈이 있는 고리가 90도씩 돈다
local function gust(f)
  return function(img)
    local gap = (f - 1) * math.pi / 2
    L.drawMask(img, maskOf(16, 16, function(x, y)
      local d, a = dist(x, y, 7.5, 7.5), math.atan(y - 7.5, x - 7.5)
      local rel = (a - gap) % (2 * math.pi)
      if d >= 5 and d <= 6.6 and rel > 1.2 then return "#" end
      if d >= 1.8 and d <= 3 and ((rel + math.pi) % (2 * math.pi)) > 2.4 then return "#" end
    end), 0, 0, "W", "G")
  end
end
saveTagged(art .. "Fx/Gust.aseprite", 16, 16, {
  { name = "Spin", ms = 70, draw = gust(1) },
  { name = "Spin", ms = 70, draw = gust(2) },
  { name = "Spin", ms = 70, draw = gust(3) },
  { name = "Spin", ms = 70, draw = gust(4) },
})

-- 로켓 16x10 (ROCKET): 오른쪽을 향한다. 뒤쪽 불꽃이 깜빡인다.
local function rocket(f)
  return function(img)
    local flame = f == 1 and { "..###", "#####", "..###" } or { "...##", ".####", "...##" }
    L.drawMask(img, flame, 0, 3, "Y", "R")
    L.drawMask(img, { "##.......", "#########", "##########", "#########", "##......." }, 5, 3, "W", "L")
    L.drawMap(img, { "RR", "RRR", "RR" }, 13, 4)
    L.px(img, 9, 5, "B")
    L.px(img, 5, 2, "r"); L.px(img, 6, 2, "r"); L.px(img, 5, 8, "r"); L.px(img, 6, 8, "r")
  end
end
saveTagged(art .. "Fx/Rocket.aseprite", 16, 10, {
  { name = "Fly", ms = 60, draw = rocket(1) },
  { name = "Fly", ms = 60, draw = rocket(2) },
})

-- 광선 8x8 (BEAM, LASER): 가로로 이어 붙이는 몸통 + 끝머리. 흰색·회색이라 Unity에서 틴트.
saveTagged(art .. "Fx/Beam.aseprite", 8, 8, {
  { name = "Segment", draw = function(img)
    L.rect(img, 0, 1, 7, 1, "K"); L.rect(img, 0, 6, 7, 6, "K")
    L.rect(img, 0, 2, 7, 2, "L"); L.rect(img, 0, 3, 7, 4, "W"); L.rect(img, 0, 5, 7, 5, "L")
  end },
  { name = "Head", draw = function(img)
    L.drawMask(img, { "####..", "######", "######", "######", "####.." }, 0, 2, "W", "L")
    L.rect(img, 0, 3, 4, 4, "W")
  end },
})

-- SHIFT 마무리 8방 화살표 16x16: 큰 ⇧
saveTagged(art .. "Fx/ShiftArrow.aseprite", 16, 16, {
  { name = "Arrow", draw = function(img)
    L.drawMask(img, {
      "......#......", ".....###.....", "....#####....", "...#######...", "..#########..", ".###########.",
      "#############", "....#####....", "....#####....", "....#####....", "....#####....", "....#####....",
    }, 1, 2, "B", "b")
    L.px(img, 7, 4, "W"); L.px(img, 6, 5, "W"); L.px(img, 5, 6, "W")
  end },
})

-- 블록이 깨질 때 흩어지는 조각 4x4
saveTagged(art .. "Fx/BlockDebris.aseprite", 4, 4, {
  { name = "Debris", ms = 100, draw = function(img) L.drawMask(img, { "##", "#." }, 1, 1, "L", "M") end },
  { name = "Debris", ms = 100, draw = function(img) L.drawMask(img, { "#", "#" }, 1, 1, "M", "M") end },
})

----------------------------------------------------------------------
-- 숫자·기호 글꼴 5x7 (PixelFont와 같은 규칙: 흰색 한 가지, 태그 없음, 글자별 한 프레임)
-- 프레임 순서(0부터): 0~9, + - ! ? : / % x(×) → ← ↑ ↓ ⇧
----------------------------------------------------------------------
local SYMBOLS = {
  { "0", { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." } },
  { "1", { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." } },
  { "2", { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" } },
  { "3", { "####.", "....#", "....#", ".###.", "....#", "....#", "####." } },
  { "4", { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." } },
  { "5", { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." } },
  { "6", { ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###." } },
  { "7", { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." } },
  { "8", { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." } },
  { "9", { ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###." } },
  { "+", { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." } },
  { "-", { ".....", ".....", ".....", "#####", ".....", ".....", "....." } },
  { "!", { "..#..", "..#..", "..#..", "..#..", "..#..", ".....", "..#.." } },
  { "?", { ".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.." } },
  { ":", { ".....", "..#..", ".....", ".....", ".....", "..#..", "....." } },
  { "/", { "....#", "....#", "...#.", "..#..", ".#...", "#....", "#...." } },
  { "%", { "##..#", "##..#", "...#.", "..#..", ".#...", "#..##", "#..##" } },
  { "x", { ".....", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "....." } },
  { "RIGHT", { ".....", "..#..", "...#.", "#####", "...#.", "..#..", "....." } },
  { "LEFT", { ".....", "..#..", ".#...", "#####", ".#...", "..#..", "....." } },
  { "UP", { "..#..", ".###.", "#.#.#", "..#..", "..#..", "..#..", "..#.." } },
  { "DOWN", { "..#..", "..#..", "..#..", "..#..", "#.#.#", ".###.", "..#.." } },
  { "SHIFT", { "..#..", ".#.#.", "#...#", "##.##", ".#.#.", ".#.#.", ".###." } },
}
local symFrames = {}
for i, s in ipairs(SYMBOLS) do
  local rows = s[2]
  symFrames[i] = { ms = 100, draw = function(img)
    for y, row in ipairs(rows) do for x = 1, #row do if row:sub(x, x) == "#" then L.px(img, x - 1, y - 1, "W") end end end
  end }
end
L.buildSprite(art .. "Font/PixelSymbols.aseprite", 5, 7, symFrames, {})

----------------------------------------------------------------------
-- UI
----------------------------------------------------------------------
-- 9-slice 판: 목표 단어 판, 결과 창 등. 테두리 3px(Unity에서 Border 3,3,3,3).
saveTagged(art .. "Ui/Panel.aseprite", 16, 16, {
  { name = "Panel", draw = function(img)
    L.rect(img, 0, 0, 15, 15, "K")
    L.rect(img, 1, 1, 14, 14, "M")
    L.rect(img, 2, 2, 13, 13, "D")
    L.rect(img, 1, 1, 14, 1, "L"); L.rect(img, 1, 1, 1, 14, "L")
    for _, p in ipairs({ { 0, 0 }, { 15, 0 }, { 0, 15 }, { 15, 15 } }) do img:drawPixel(p[1], p[2], app.pixelColor.rgba(0, 0, 0, 0)) end
  end },
})

-- 손 HP 바 한 칸 4x8: 이어 붙여 쓴다
saveTagged(art .. "Ui/HpBar.aseprite", 4, 8, {
  { name = "Fill", draw = function(img) L.rect(img, 0, 0, 3, 0, "K"); L.rect(img, 0, 7, 3, 7, "K"); L.rect(img, 0, 1, 3, 1, "W"); L.rect(img, 0, 2, 3, 5, "R"); L.rect(img, 0, 6, 3, 6, "r") end },
  { name = "Empty", draw = function(img) L.rect(img, 0, 0, 3, 0, "K"); L.rect(img, 0, 7, 3, 7, "K"); L.rect(img, 0, 1, 3, 6, "D") end },
})

-- Space 키 아이콘 24x14: "슬롯에서 Space" 안내용. 눌린 모양은 2px 내려간다.
local function spaceKey(press)
  return function(img)
    local y0, bottom = press, 13
    L.rect(img, 1, y0, 22, y0, "K"); L.rect(img, 1, bottom, 22, bottom, "K")
    L.rect(img, 0, y0 + 1, 0, bottom - 1, "K"); L.rect(img, 23, y0 + 1, 23, bottom - 1, "K")
    L.rect(img, 1, y0 + 1, 22, y0 + 9, "M"); L.rect(img, 1, y0 + 1, 22, y0 + 1, "L"); L.rect(img, 1, y0 + 1, 1, y0 + 9, "L")
    if y0 + 10 <= bottom - 1 then L.rect(img, 1, y0 + 10, 22, bottom - 1, "D") end
    -- ␣ 기호
    L.rect(img, 8, y0 + 5, 8, y0 + 7, "W"); L.rect(img, 15, y0 + 5, 15, y0 + 7, "W"); L.rect(img, 8, y0 + 7, 15, y0 + 7, "W")
  end
end
saveTagged(art .. "Ui/SpaceKey.aseprite", 24, 14, {
  { name = "Base", draw = spaceKey(0) },
  { name = "Pressed", draw = spaceKey(2) },
})

-- 마법 아이콘 12x12: 공격 블록 위, 단어 판 옆에 쓴다. 태그 = attackPatterns.json spells의 icon 값.
local function icon(rows, ox, oy, fill, shade, extra)
  return function(img) L.drawMask(img, rows, ox, oy, fill, shade); if extra then extra(img) end end
end
saveTagged(art .. "Ui/SpellIcon.aseprite", 12, 12, {
  { name = "Fire", draw = icon({ "...#....", "..##....", "..###.#.", ".####.##", ".#######", "########", "########", ".######.", "..####.." }, 2, 1, "R", "r",
    function(img) L.drawMap(img, { "..Y...", ".YYY..", "YYYYY.", ".YWY.." }, 4, 6) end) },
  { name = "Bolt", draw = icon({ "....###", "...###.", "..###..", ".######", "...###.", "..###..", ".##....", "#......" }, 2, 2, "W", "Y") },
  { name = "Beam", draw = icon({ ".##.......", "##########", "##########", ".##......." }, 1, 4, "W", "B",
    function(img) L.px(img, 2, 5, "B"); L.px(img, 3, 6, "B") end) },
  { name = "Gust", draw = function(img)
    L.drawMask(img, maskOf(12, 12, function(x, y)
      local d, a = dist(x, y, 5.5, 5.5), math.atan(y - 5.5, x - 5.5)
      if d >= 3.6 and d <= 4.9 and a > -2.2 then return "#" end
      if d >= 0.9 and d <= 2 and a < 1.2 then return "#" end
    end), 0, 0, "W", "G")
  end },
  { name = "Star", draw = icon({ "....#....", "....#....", "...###...", "#########", ".#######.", "..#####..", "..##.##..", ".##...##.", "#.......#" }, 1, 1, "P", "r",
    function(img) L.px(img, 5, 5, "W") end) },
  { name = "Ice", draw = icon({ "...#...", "..###..", "..###..", ".#####.", ".#####.", "..###..", "..###..", "...#..." }, 2, 1, "B", "b",
    function(img) L.px(img, 4, 3, "W"); L.px(img, 4, 4, "W"); L.px(img, 4, 5, "W") end) },
  { name = "Blade", draw = icon({ ".......##", "......###", ".....###.", "....###..", "#..###...", ".####....", "..##.....", ".#..#....", "#........" }, 1, 1, "W", "L") },
  { name = "Rocket", draw = function(img)
    L.drawMask(img, { "..##..", ".####.", ".####.", ".####.", ".####.", "######", "#.##.#" }, 3, 1, "W", "L")
    L.drawMap(img, { "RR", "RRRR" }, 5, 1); L.px(img, 6, 4, "B")
    L.drawMap(img, { "YY", ".Y" }, 5, 8)
  end },
  { name = "Dragon", draw = function(img)
    -- 불을 내뿜는 원뿔: 왼쪽 좁고 오른쪽 넓게
    L.drawMask(img, maskOf(12, 12, function(x, y) if x >= 1 and x <= 10 and math.abs(y - 5.5) <= 0.6 + x * 0.42 then return "#" end end), 0, 0, "R", "r")
    L.drawMap(img, maskOf(12, 12, function(x, y) if x >= 2 and x <= 8 and math.abs(y - 5.5) <= x * 0.22 then return "Y" end end), 0, 0)
    L.px(img, 2, 5, "W"); L.px(img, 3, 5, "W")
  end },
  { name = "Shift", draw = icon({ "....#....", "...###...", "..#####..", ".#######.", "#########", "..#####..", "..#####..", "..#####.." }, 1, 2, "B", "b") },
})

print("FSS v5 build done")
