-- Finger Snap Shift 도트 에셋 공용 규칙.
-- 모든 에셋이 같은 팔레트, 같은 외곽선·음영 규칙을 쓰도록 여기 한 곳에 모아 둔다.

local L = {}

-- 고정 팔레트 12색. 이 색 말고는 쓰지 않는다. (기호, 이름, R, G, B)
L.PALETTE = {
  { "K", "Black",      0,   0,   0   },
  { "W", "White",      255, 255, 255 },
  { "L", "LightGray",  194, 195, 199 },
  { "M", "MidGray",    122, 123, 134 },
  { "D", "DarkGray",   62,  63,  74  },
  { "R", "Red",        232, 52,  63  },
  { "r", "DarkRed",    126, 31,  61  },
  { "Y", "Yellow",     255, 212, 59  },
  { "B", "Sky",        92,  203, 255 },
  { "b", "DarkSky",    42,  120, 181 },
  { "G", "Green",      76,  217, 100 },
  { "P", "Pink",       255, 122, 176 },
}

L.C = {}
for _, p in ipairs(L.PALETTE) do
  L.C[p[1]] = app.pixelColor.rgba(p[3], p[4], p[5], 255)
end

L.TILE_W = 16
L.TILE_H = 20      -- 윗면 16 + 두께 면 3 + 아래 외곽선 1
L.TOP_H = 16

function L.makePalette()
  local pal = Palette(#L.PALETTE + 1)
  pal:setColor(0, Color { r = 0, g = 0, b = 0, a = 0 })
  for i, p in ipairs(L.PALETTE) do
    pal:setColor(i, Color { r = p[3], g = p[4], b = p[5], a = 255 })
  end
  return pal
end

function L.writeGpl(path)
  local f = assert(io.open(path, "w"))
  f:write("GIMP Palette\nName: Finger Snap Shift\nColumns: 6\n#\n")
  for _, p in ipairs(L.PALETTE) do
    f:write(string.format("%3d %3d %3d\t%s\n", p[3], p[4], p[5], p[2]))
  end
  f:close()
end

-- 프레임 여러 장짜리 스프라이트를 만든다.
-- frames: { { draw = function(img) ... end, ms = 지속시간 }, ... }
-- tags: { { "이름", 시작프레임, 끝프레임 }, ... }
function L.buildSprite(path, w, h, frames, tags)
  local spr = Sprite(w, h, ColorMode.RGB)
  spr:setPalette(L.makePalette())
  local layer = spr.layers[1]
  layer.name = "Art"
  for i, fr in ipairs(frames) do
    if i > 1 then spr:newEmptyFrame() end
    local img = Image(w, h, ColorMode.RGB)
    fr.draw(img)
    if layer:cel(i) then
      layer:cel(i).image = img
    else
      spr:newCel(layer, i, img, Point(0, 0))
    end
    spr.frames[i].duration = (fr.ms or 100) / 1000
  end
  for _, t in ipairs(tags or {}) do
    local tag = spr:newTag(t[2], t[3])
    tag.name = t[1]
  end
  spr:saveAs(path)
  spr:close()
end

function L.px(img, x, y, ch)
  if x >= 0 and y >= 0 and x < img.width and y < img.height then
    img:drawPixel(x, y, L.C[ch])
  end
end

function L.rect(img, x0, y0, x1, y1, ch)
  for y = y0, y1 do
    for x = x0, x1 do L.px(img, x, y, ch) end
  end
end

-- 색 기호를 그대로 찍는 그림. '.'은 투명.
function L.drawMap(img, rows, ox, oy)
  for y, row in ipairs(rows) do
    for x = 1, #row do
      local ch = row:sub(x, x)
      if ch ~= "." and L.C[ch] then L.px(img, ox + x - 1, oy + y - 1, ch) end
    end
  end
end

-- 실루엣 마스크로 그리는 그림. 외곽선과 음영을 규칙으로 자동 처리한다.
--   '#' 몸통(오른쪽·아래가 비면 음영색)  'w' 몸통(항상 기본색)  'l' 몸통(항상 음영색)
--   'k' 안쪽 검은 선  그 밖의 팔레트 기호는 외곽선 없이 그대로 찍는다.
-- 검은 1px 외곽선은 몸통의 상하좌우 바깥에 붙는다. 빛은 왼쪽 위.
function L.drawMask(img, rows, ox, oy, fill, shade)
  local function at(x, y)
    local row = rows[y]
    if not row or x < 1 or x > #row then return "." end
    return row:sub(x, x)
  end
  local function body(x, y)
    local c = at(x, y)
    return c == "#" or c == "w" or c == "l"
  end
  local w = 0
  for _, row in ipairs(rows) do if #row > w then w = #row end end
  for y = 0, #rows + 1 do
    for x = 0, w + 1 do
      local c = at(x, y)
      local out = nil
      if c == "." then
        if body(x - 1, y) or body(x + 1, y) or body(x, y - 1) or body(x, y + 1) then out = "K" end
      elseif c == "#" then
        if body(x + 1, y) and body(x, y + 1) then out = fill else out = shade end
      elseif c == "w" then
        out = fill
      elseif c == "l" then
        out = shade
      elseif c == "k" then
        out = "K"
      elseif L.C[c] then
        out = c
      end
      if out then L.px(img, ox + x - 1, oy + y - 1, out) end
    end
  end
end

-- 키캡. 윗면(밝은 테두리 = 왼쪽 위 빛) + 아래 두께 면.
-- o.press: 눌린 깊이(px). 깊을수록 윗면이 내려가고 두께 면이 줄어든다.
function L.drawKey(img, o)
  local top, hi, side = o.top or "M", o.hi or "L", o.side or "D"
  local y0 = o.press or 0
  local bottom = L.TILE_H - 1
  L.rect(img, 1, y0, 14, y0, "K")
  L.rect(img, 1, bottom, 14, bottom, "K")
  L.rect(img, 0, y0 + 1, 0, bottom - 1, "K")
  L.rect(img, 15, y0 + 1, 15, bottom - 1, "K")
  L.rect(img, 1, y0 + 1, 14, y0 + 15, top)
  L.rect(img, 1, y0 + 1, 14, y0 + 1, hi)
  L.rect(img, 1, y0 + 1, 1, y0 + 15, hi)
  if y0 + 16 <= bottom - 1 then
    L.rect(img, 1, y0 + 16, 14, bottom - 1, side)
    if o.seam then L.rect(img, 1, y0 + 16, 14, y0 + 16, "K") end
  end
end

-- 5x7 대문자 글꼴. 직접 그린 것.
L.GLYPHS = {
  A = { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
  B = { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
  C = { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." },
  D = { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
  E = { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
  F = { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
  G = { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###." },
  H = { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
  I = { ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." },
  J = { "..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##.." },
  K = { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
  L = { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
  M = { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" },
  N = { "#...#", "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#" },
  O = { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
  P = { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
  Q = { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" },
  R = { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
  S = { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
  T = { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
  U = { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
  V = { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." },
  W = { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "#.#.#", ".#.#." },
  X = { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" },
  Y = { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." },
  Z = { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" },
}

function L.drawGlyph(img, letter, ox, oy, ch)
  local g = L.GLYPHS[letter]
  for y, row in ipairs(g) do
    for x = 1, #row do
      if row:sub(x, x) == "#" then L.px(img, ox + x - 1, oy + y - 1, ch or "W") end
    end
  end
end

-- 저장된 .aseprite를 읽어 프레임 그림과 태그를 돌려준다. 같은 파일은 한 번만 연다.
-- 결과: { w, h, frames = { Image... }, tags = { 이름 = { from, to } }, order = { 이름... } }
L._files = {}
function L.load(path)
  if L._files[path] then return L._files[path] end
  local spr = Sprite { fromFile = path }
  local info = { w = spr.width, h = spr.height, frames = {}, tags = {}, order = {} }
  for i = 1, #spr.frames do
    local img = Image(spr.width, spr.height, ColorMode.RGB)
    img:drawSprite(spr, i)
    info.frames[i] = img
  end
  for _, tag in ipairs(spr.tags) do
    info.tags[tag.name] = { tag.fromFrame.frameNumber, tag.toFrame.frameNumber }
    info.order[#info.order + 1] = tag.name
  end
  spr:close()
  L._files[path] = info
  return info
end

-- 그림을 다른 그림 위에 겹쳐 그린다(투명 픽셀은 건너뜀).
function L.blit(dst, img, ox, oy)
  for y = 0, img.height - 1 do
    for x = 0, img.width - 1 do
      local c = img:getPixel(x, y)
      if app.pixelColor.rgbaA(c) > 0 then
        local dx, dy = ox + x, oy + y
        if dx >= 0 and dy >= 0 and dx < dst.width and dy < dst.height then
          dst:drawPixel(dx, dy, c)
        end
      end
    end
  end
end

-- 태그 이름과 그 안의 순번으로 프레임을 겹쳐 그린다. tagName이 nil이면 n은 전체 프레임 번호.
function L.blitTag(dst, path, tagName, n, ox, oy)
  local info = L.load(path)
  local frame = n or 1
  if tagName then
    local t = assert(info.tags[tagName], "no tag " .. tagName .. " in " .. path)
    frame = t[1] + (n or 1) - 1
  end
  L.blit(dst, info.frames[frame], ox, oy)
end

function L.drawText(img, text, ox, oy, ch)
  for i = 1, #text do
    local c = text:sub(i, i)
    if L.GLYPHS[c] then L.drawGlyph(img, c, ox + (i - 1) * 6, oy, ch) end
  end
end

-- 정수배 nearest 확대 후 PNG로 저장한다.
function L.savePngScaled(img, path, scale)
  local spr = Sprite(img.width * scale, img.height * scale, ColorMode.RGB)
  local big = Image(img.width * scale, img.height * scale, ColorMode.RGB)
  for y = 0, img.height - 1 do
    for x = 0, img.width - 1 do
      local c = img:getPixel(x, y)
      for sy = 0, scale - 1 do
        for sx = 0, scale - 1 do
          big:drawPixel(x * scale + sx, y * scale + sy, c)
        end
      end
    end
  end
  spr:newCel(spr.layers[1], 1, big, Point(0, 0))
  spr:saveCopyAs(path)
  spr:close()
end

return L
