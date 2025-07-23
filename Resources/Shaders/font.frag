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
    float distance = texture(uTexture, TexCoords).r;

    // Use the adjustable threshold instead of a hardcoded 0.5
    float alpha = smoothstep(uThreshold - uSmoothing, uThreshold + uSmoothing, distance);

    FragColor = vec4(vec3(uColor), alpha);