use image::{Rgba, RgbaImage};
use std::collections::HashMap;

use crate::models::settings::{DisplayMode, IconTheme};
use crate::models::usage::LimitType;

const ICON_SIZE: u32 = 32;
const MAX_CACHE: usize = 50;

// ---------------------------------------------------------------------------
// 5x7 비트맵 글리프 (행 단위, MSB가 왼쪽)
// ---------------------------------------------------------------------------
//  각 글리프: [u8; 7], 5비트만 사용 (bit4=leftmost)

const GLYPH_0: [u8; 7] = [0b01110, 0b10001, 0b10011, 0b10101, 0b11001, 0b10001, 0b01110];
const GLYPH_1: [u8; 7] = [0b00100, 0b01100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110];
const GLYPH_2: [u8; 7] = [0b01110, 0b10001, 0b00001, 0b00010, 0b00100, 0b01000, 0b11111];
const GLYPH_3: [u8; 7] = [0b11111, 0b00010, 0b00100, 0b00010, 0b00001, 0b10001, 0b01110];
const GLYPH_4: [u8; 7] = [0b00010, 0b00110, 0b01010, 0b10010, 0b11111, 0b00010, 0b00010];
const GLYPH_5: [u8; 7] = [0b11111, 0b10000, 0b11110, 0b00001, 0b00001, 0b10001, 0b01110];
const GLYPH_6: [u8; 7] = [0b00110, 0b01000, 0b10000, 0b11110, 0b10001, 0b10001, 0b01110];
const GLYPH_7: [u8; 7] = [0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b01000, 0b01000];
const GLYPH_8: [u8; 7] = [0b01110, 0b10001, 0b10001, 0b01110, 0b10001, 0b10001, 0b01110];
const GLYPH_9: [u8; 7] = [0b01110, 0b10001, 0b10001, 0b01111, 0b00001, 0b00010, 0b01100];
// "%" 기호
const GLYPH_PCT: [u8; 7] = [0b11000, 0b11001, 0b00010, 0b00100, 0b01000, 0b10011, 0b00011];
// "F" (Full / 100% 표시용 첫 글자)
const GLYPH_F: [u8; 7] = [0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b10000];
// "L"
const GLYPH_L: [u8; 7] = [0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b11111];

fn digit_glyph(d: u32) -> &'static [u8; 7] {
    match d {
        0 => &GLYPH_0,
        1 => &GLYPH_1,
        2 => &GLYPH_2,
        3 => &GLYPH_3,
        4 => &GLYPH_4,
        5 => &GLYPH_5,
        6 => &GLYPH_6,
        7 => &GLYPH_7,
        8 => &GLYPH_8,
        9 => &GLYPH_9,
        _ => &GLYPH_0,
    }
}

// ---------------------------------------------------------------------------
// IconRenderer
// ---------------------------------------------------------------------------

pub struct IconRenderer {
    cache: HashMap<String, Vec<u8>>,
}

impl Default for IconRenderer {
    fn default() -> Self {
        Self::new()
    }
}

impl IconRenderer {
    pub fn new() -> Self {
        Self {
            cache: HashMap::new(),
        }
    }

    /// RGBA 바이트 벡터 반환 (32×32)
    pub fn render(
        &mut self,
        percentage: f64,
        limit_type: &LimitType,
        theme: &IconTheme,
        display_mode: &DisplayMode,
    ) -> Vec<u8> {
        let pct_int = (percentage.clamp(0.0, 100.0)).round() as u32;
        let cache_key = format!("{}-{:?}-{:?}-{:?}", pct_int, limit_type, theme, display_mode);

        if let Some(cached) = self.cache.get(&cache_key) {
            return cached.clone();
        }

        let rgba = self.render_icon(pct_int, percentage, limit_type, theme, display_mode);

        if self.cache.len() >= MAX_CACHE {
            self.cache.clear();
        }
        self.cache.insert(cache_key, rgba.clone());

        rgba
    }

    fn render_icon(
        &self,
        pct_int: u32,
        percentage: f64,
        limit_type: &LimitType,
        theme: &IconTheme,
        display_mode: &DisplayMode,
    ) -> Vec<u8> {
        let mut img = RgbaImage::new(ICON_SIZE, ICON_SIZE);
        let (r, g, b) = limit_type.color_for_percentage(percentage);

        match theme {
            IconTheme::ColorTranslucent => {
                self.draw_circle(&mut img, Rgba([r, g, b, 200]));
            }
            IconTheme::ColorWithBackground => {
                self.fill_background(&mut img, Rgba([40, 40, 40, 255]));
                self.draw_circle(&mut img, Rgba([r, g, b, 255]));
            }
            IconTheme::Monochrome => {
                self.draw_circle(&mut img, Rgba([255, 255, 255, 255]));
            }
        }

        match display_mode {
            DisplayMode::PercentOnly | DisplayMode::IconAndPercent => {
                let text_color = match theme {
                    IconTheme::Monochrome => Rgba([0, 0, 0, 255]),
                    _ => Rgba([255, 255, 255, 255]),
                };
                self.draw_percentage_text(&mut img, pct_int, text_color);
            }
            DisplayMode::IconOnly => {}
        }

        img.into_raw()
    }

    /// 32×32 원을 그린다 (간단한 안티앨리어싱)
    fn draw_circle(&self, img: &mut RgbaImage, color: Rgba<u8>) {
        let cx = (ICON_SIZE as f32) / 2.0;
        let cy = (ICON_SIZE as f32) / 2.0;
        let r = cx - 1.5; // 반지름 (약간 여백)

        for y in 0..ICON_SIZE {
            for x in 0..ICON_SIZE {
                let dx = x as f32 + 0.5 - cx;
                let dy = y as f32 + 0.5 - cy;
                let dist = (dx * dx + dy * dy).sqrt();

                // 안티앨리어싱: 경계 0.8픽셀 범위에서 알파 감소
                let aa_alpha = if dist <= r - 0.8 {
                    1.0_f32
                } else if dist <= r + 0.8 {
                    (r + 0.8 - dist) / 1.6
                } else {
                    0.0
                };

                if aa_alpha > 0.0 {
                    let final_alpha = (color[3] as f32 * aa_alpha) as u8;
                    img.put_pixel(x, y, Rgba([color[0], color[1], color[2], final_alpha]));
                }
            }
        }
    }

    /// 배경 전체를 단색으로 채운다
    fn fill_background(&self, img: &mut RgbaImage, color: Rgba<u8>) {
        for y in 0..ICON_SIZE {
            for x in 0..ICON_SIZE {
                img.put_pixel(x, y, color);
            }
        }
    }

    /// 퍼센트 텍스트를 아이콘 중앙에 그린다
    /// - 100%: "FL" 두 글자
    /// - 0-9%:  "N%" 두 글자 (단, 작은 글리프를 씀)
    /// - 10-99%: "NN%" — 세 글자 → 글리프 스케일 조정
    fn draw_percentage_text(&self, img: &mut RgbaImage, pct: u32, color: Rgba<u8>) {
        if pct >= 100 {
            // "FL" 두 글자, scale=2
            let scale = 2u32;
            let glyph_w = 5 * scale;
            let gap = scale;
            let total_w = glyph_w * 2 + gap;
            let glyph_h = 7 * scale;
            let start_x = ((ICON_SIZE - total_w) / 2) as i32;
            let start_y = ((ICON_SIZE - glyph_h) / 2) as i32;
            self.draw_glyph(img, &GLYPH_F, start_x, start_y, scale, color);
            self.draw_glyph(img, &GLYPH_L, start_x + (glyph_w + gap) as i32, start_y, scale, color);
            return;
        }

        if pct < 10 {
            // "N%" scale=2
            let scale = 2u32;
            let glyph_w = 5 * scale;
            let gap = scale;
            let total_w = glyph_w * 2 + gap;
            let glyph_h = 7 * scale;
            let start_x = ((ICON_SIZE - total_w) / 2) as i32;
            let start_y = ((ICON_SIZE - glyph_h) / 2) as i32;
            self.draw_glyph(img, digit_glyph(pct), start_x, start_y, scale, color);
            self.draw_glyph(img, &GLYPH_PCT, start_x + (glyph_w + gap) as i32, start_y, scale, color);
            return;
        }

        // 10-99: "NN%" three glyphs, scale=1 with 2x vertical stretch for readability
        // scale=1 but draw each pixel as 1x2 to be legible
        let scale_x = 1u32;
        let scale_y = 2u32;
        let glyph_w = 5 * scale_x;
        let glyph_h = 7 * scale_y;
        let gap = 1u32;
        let total_w = glyph_w * 3 + gap * 2;
        let start_x = ((ICON_SIZE - total_w) / 2) as i32;
        let start_y = ((ICON_SIZE - glyph_h) / 2) as i32;

        let tens = pct / 10;
        let ones = pct % 10;

        self.draw_glyph_xy(img, digit_glyph(tens), start_x, start_y, scale_x, scale_y, color);
        self.draw_glyph_xy(img, digit_glyph(ones), start_x + (glyph_w + gap) as i32, start_y, scale_x, scale_y, color);
        self.draw_glyph_xy(img, &GLYPH_PCT, start_x + ((glyph_w + gap) * 2) as i32, start_y, scale_x, scale_y, color);
    }

    /// 정사각 스케일로 글리프를 그린다
    fn draw_glyph(
        &self,
        img: &mut RgbaImage,
        glyph: &[u8; 7],
        ox: i32,
        oy: i32,
        scale: u32,
        color: Rgba<u8>,
    ) {
        self.draw_glyph_xy(img, glyph, ox, oy, scale, scale, color);
    }

    /// x/y 스케일이 다른 경우의 글리프 렌더링
    #[allow(clippy::too_many_arguments)]
    fn draw_glyph_xy(
        &self,
        img: &mut RgbaImage,
        glyph: &[u8; 7],
        ox: i32,
        oy: i32,
        scale_x: u32,
        scale_y: u32,
        color: Rgba<u8>,
    ) {
        for (row, &bits) in glyph.iter().enumerate() {
            for col in 0..5u32 {
                let bit = (bits >> (4 - col)) & 1;
                if bit == 0 {
                    continue;
                }
                for sy in 0..scale_y {
                    for sx in 0..scale_x {
                        let px = ox + (col * scale_x + sx) as i32;
                        let py = oy + (row as u32 * scale_y + sy) as i32;
                        if px >= 0 && py >= 0 && px < ICON_SIZE as i32 && py < ICON_SIZE as i32 {
                            img.put_pixel(px as u32, py as u32, color);
                        }
                    }
                }
            }
        }
    }
}

pub const RENDERED_ICON_SIZE: u32 = ICON_SIZE;
