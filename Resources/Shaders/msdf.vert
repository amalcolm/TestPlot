#version 330 core

// Input vertex data, different for each vertex.
layout(location = 0) in vec2 aPosition; // Vertex position in screen-space
layout(location = 1) in vec2 aTexCoord; // Texture coordinate for the font atlas

// An output variable that will be passed to the fragment shader.
// OpenGL interpolates this value between vertices.
out vec2 vTexCoord;

// A uniform means this value is the same for all vertices in a single draw call.
// It's our transformation matrix to position the text correctly on the screen.
uniform mat4 uTransform;

void main()
{
    // Standard vertex shader operation: transform the vertex position.
    gl_Position = uTransform * vec4(aPosition, 0.0, 1.0);
    
    // Pass the texture coordinate straight through to the fragment shader.
    vTexCoord = aTexCoord;
}
