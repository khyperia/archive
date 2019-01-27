#include "planck.h"
#include "action_layer.h"
#ifdef AUDIO_ENABLE
  #include "audio.h"
#endif
#include "eeconfig.h"

extern keymap_config_t keymap_config;

#define _QWERTY 0
#define _SYMBOL 1
#define _NUM 2

enum planck_keycodes {
  QWERTY = SAFE_RANGE,
  SYMBOL,
  NUM,
  MY_MUSIC,
};

// Fillers to make layering more clear
#define _______ KC_TRNS
#define XXXXXXX KC_NO

const uint16_t PROGMEM keymaps[][MATRIX_ROWS][MATRIX_COLS] = {

[_QWERTY] = {
  {KC_TAB,  KC_Q,    KC_W,    KC_E,    KC_R,    KC_T,    KC_Y,    KC_U,    KC_I,    KC_O,    KC_P,    KC_BSPC},
  {SYMBOL,  KC_A,    KC_S,    KC_D,    KC_F,    KC_G,    KC_H,    KC_J,    KC_K,    KC_L,    KC_SCLN, KC_QUOT},
  {KC_LSFT, KC_Z,    KC_X,    KC_C,    KC_V,    KC_B,    KC_N,    KC_M,    KC_COMM, KC_DOT,  KC_SLSH, KC_ENT },
  {KC_LCTL, KC_LGUI, KC_LALT, KC_GRV,  KC_DELT, KC_BSPC, KC_SPC,  KC_ESC,  NUM,     KC_RALT, KC_RGUI, KC_RCTL}
},
[_SYMBOL] = {
  {KC_GRV,  _______, _______, _______, _______, _______, _______, KC_PGUP, KC_UP,   KC_PGDN, KC_MINS, KC_EQL },
  {_______, KC_RSFT, KC_VOLD, KC_VOLU, KC_MUTE, MY_MUSIC,KC_HOME, KC_LEFT, KC_DOWN, KC_RIGHT,KC_INS,  KC_BSLS},
  {_______, _______, _______, _______, _______, _______, KC_END,  KC_LCBR, KC_RCBR, KC_LBRC, KC_RBRC, _______},
  {_______, _______, _______, _______, _______, _______, _______, _______, _______, _______, _______, _______}
},
[_NUM] = {
  {_______, KC_F1,   KC_F2,   KC_F3,   KC_F4,   KC_F5,   KC_F6,   KC_F7,   KC_F8,   KC_F9,   KC_F10,  KC_F11 },
  {_______, KC_1,    KC_2,    KC_3,    KC_4,    KC_5,    KC_6,    KC_7,    KC_8,    KC_9,    KC_0,    KC_F12 },
  {_______, _______, _______, _______, _______, _______, _______, _______, _______, _______, _______, _______},
  {_______, _______, _______, _______, _______, _______, _______, _______, _______, _______, _______, _______}
}
};

#ifdef AUDIO_ENABLE

float tone_startup[][2]    = SONG(STARTUP_SOUND);
float tone_qwerty[][2]     = SONG(QWERTY_SOUND);
float music_scale[][2]     = SONG(MUSIC_SCALE_SOUND);

float tone_goodbye[][2] = SONG(GOODBYE_SOUND);
#endif

void persistant_default_layer_set(uint16_t default_layer) {
  eeconfig_update_default_layer(default_layer);
  default_layer_set(default_layer);
}

bool process_record_user(uint16_t keycode, keyrecord_t *record) {
    // #ifdef AUDIO_ENABLE
    //   PLAY_NOTE_ARRAY(tone_qwerty, false, 0);
    // #endif
    if (keycode == MY_MUSIC)
    {
        layer_off(_SYMBOL);
        if (record->event.pressed)
        {
            if (!is_audio_on())
            {
                audio_on();
            }
        }
        return false;
    }
    if (is_audio_on())
    {
        uint8_t row = record->event.key.row;
        uint8_t col = record->event.key.col;
        if (record->event.pressed && row == 3 && col == 11)
        {
            stop_all_notes();
            audio_off();
            return false;
        }
        if (record->event.pressed && row == 3 && col == 10)
        {
            voice_iterate();
            return false;
        }
        int note_idx = col + (1 - (int)row) * 12;
        const float middle_c = 440;
        float freq = middle_c * pow(2.0, (float)note_idx / 12.0);
        if (record->event.pressed)
        {
            play_note(freq, 0xf);
        }
        else
        {
            stop_note(freq);
        }
        return false;
    }
    switch (keycode) {
        case MY_MUSIC:
            return false;
        case SYMBOL:
        if (record->event.pressed) {
            layer_on(_SYMBOL);
        } else {
            layer_off(_SYMBOL);
        }
        return false;
        case NUM:
        if (record->event.pressed) {
            layer_on(_NUM);
        } else {
            layer_off(_NUM);
        }
        return false;
    }
    return true;
}

void matrix_init_user(void) {
    #ifdef AUDIO_ENABLE
        startup_user();
    #endif
}

#ifdef AUDIO_ENABLE

void startup_user()
{
    //_delay_ms(20); // gets rid of tick
    //PLAY_NOTE_ARRAY(tone_startup, false, 0);
}

void shutdown_user()
{
    //PLAY_NOTE_ARRAY(tone_goodbye, false, 0);
    //_delay_ms(150);
    //stop_all_notes();
}

void music_on_user(void)
{
    //music_scale_user();
}

void music_scale_user(void)
{
    //PLAY_NOTE_ARRAY(music_scale, false, 0);
}

#endif
