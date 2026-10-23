# CAFECITA: model sheet

You produce sprite sheets for a 2D retro arcade fighting game. Use your image generation tool for every image below.

## Character
An ORIGINAL Italian-brainrot style surreal character, Mexican edition: a slim ballerina whose HEAD is a small brown clay 'jarrito' pot of cafe de olla with painted white flowers and steam rising from the top (two cute eyes and a smile painted on the pot), a pink tutu decorated with colourful papel picado patterns, a pink leotard, long thin legs with pink ballet pointe shoes tied with ribbons, and a cinnamon stick held in her RIGHT hand like a dagger. Graceful ballet body language: pirouettes, arabesques, pointed toes, absurd and creepy at the same time.

## Reference images (in this directory)
- none yet: this batch creates `model_sheet.png`.

## Layout rules (critical: the game slices every image automatically)
- EDIT the given template image and keep its canvas exactly: 2172x724, flat magenta (#FF00FF) background,
  green guide lines untouched.
- The template has N equal cells separated by green vertical lines. Draw exactly one frame per cell, left to right
  in chronological order. Never cross the separators.
- Solid green line = the floor: feet stand on it. Dashed green line = the top of the head when standing upright.
  Small green tick under the floor = horizontal centre: keep the hips above it. The character animates IN PLACE
  (the game moves him); attacks extend forward from that spot.
- The character FACES RIGHT in every frame.
- Same character, same size, same palette in every frame and in every image. Consecutive frames differ only by a
  small, smooth change of pose so the animation is fluid. Loops connect the last frame back to the first.
- No text, labels, numbers, cast shadows, floor texture or scenery.
- The guides do not have to survive pixel-perfect: if the generator shifts or redraws them slightly, SAVE the
  image anyway (the game detects the guides). Only reject a result when a frame is missing or two frames merge.

## Style
Detailed 16-bit arcade fighting-game pixel art (Mortal Kombat II / Street Fighter Alpha era sprites): crisp hard
pixels, no blur, no anti-aliasing, limited palette, 1-pixel dark outline, light from the upper left. Gory horror
tone: blood is bright red, wet and abundant.

## Images (in this order)

### `model_sheet.png`
No template: one full-body neutral fighting stance, facing right, holding the cinnamon stick, flat magenta (#FF00FF) background. This becomes the reference for every other image, so make it clean and detailed.

## Output
Skip any image whose output file already exists. For every new image, copy the ORIGINAL generated PNG to the exact
path given (create folders as needed). Copy the exact file path the image tool returned for THAT generation; never
pick "the newest file" in ~/.codex/generated_images, other sessions write there at the same time.
Copy it unchanged: no resizing, cropping or recolouring. If a generation fails or
ignores the template, retry it once, then continue with the next one. Do not write any code for the game.
When finished, print the list of files written.
