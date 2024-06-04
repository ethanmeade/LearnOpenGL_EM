#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;

uniform mat4 model;

//out vec2 TexCoords;
// out vec3 gsNormal;

void main()
{
    vec4 worldPos = model * vec4(aPos, 1.0);
    gl_Position = worldPos;

    // TexCoords = aTexCoords;
    
    // mat3 normalMatrix = transpose(inverse(mat3(model)));
    // gsNormal = normalMatrix * aNormal;
}