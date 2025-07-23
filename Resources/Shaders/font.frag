#version 330 core

in vec2 TexCoords;
out vec4 FragColor;

uniform sampler2D uTexture;
uniform vec4 uColor;
uniform float uSmoothing;

// New uniform to control the edge threshold
uniform float uThreshold;

void main()
{
    vec3 tex = texture(uTexture, TexCoords).rgb;
    if (tex.r < 0.0001) discard;

    FragColor = vec4(1-tex, tex.r);
 }