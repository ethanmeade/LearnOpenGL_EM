#version 330 core
out vec4 FragColor;

in VS_OUT {
    vec3 FragPos;
    vec3 Normal;
    vec2 TexCoords;
    vec4 FragPosLightSpace;
} fs_in;

uniform sampler2D diffuseTexture;
uniform sampler2D shadowMap;
uniform sampler2D gPosition;

struct Light {
    vec3 position;  
    vec3 direction;
    float cutOff;
    float outerCutOff;
};

// uniform vec3 lightPos;
uniform Light light;
uniform vec3 viewPos;

uniform float far_plane;
uniform bool shadows;

// array of offset direction for sampling
vec2 gridSamplingDisk[9] = vec2[]
(
   vec2(1, -1), vec2( 1, 0), vec2(1, 1), 
   vec2( 0, -1), vec2(0, 0), vec2(0, 1),
   vec2(-1, -1), vec2(-1, 0), vec2(-1, 1)
);

float ShadowCalculation(vec4 fragPosLightSpace)
{
    // perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;
    // get closest depth value from light's perspective (using [0,1] range fragPosLight as coords)
    float closestDepth = texture(shadowMap, projCoords.xy).r; 
    // get depth of current fragment from light's perspective
    float currentDepth = projCoords.z;
    // calculate bias (based on depth map resolution and slope)
    vec3 normal = normalize(fs_in.Normal);
    vec3 lightDir = normalize(light.position - fs_in.FragPos);
    float bias = max(0.05 * (1.0 - dot(normal, lightDir)), 0.005);
    // check whether current frag pos is in shadow
    // float shadow = currentDepth - bias > closestDepth  ? 1.0 : 0.0;
    // PCF
    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
    for(int x = -1; x <= 1; ++x)
    {
        for(int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(shadowMap, projCoords.xy + vec2(x, y) * texelSize).r; 
            shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0;        
        }    
    }
    shadow /= 9.0;
    
    // keep the shadow at 0.0 when outside the far_plane region of the light's frustum.
    if(projCoords.z > 1.0)
        shadow = 0.0;
        
    return shadow;
}

// float ShadowCalculation(vec3 fragPos)
// {
//     // get vector between fragment position and light position
//     // vec3 fragToLight = fragPos - light.position;
//     vec3 fragToLight = fragPos - lightPos;
//     // use the fragment to light vector to sample from the depth map    
//     // float closestDepth = texture(depthMap, fragToLight).r;
//     // it is currently in linear range between [0,1], let's re-transform it back to original depth value
//     // closestDepth *= far_plane;
//     // now get current linear depth as the length between the fragment and light position
//     float currentDepth = length(fragToLight);
//     float shadow = 0.0;
//     float bias = 0.15;
//     int samples = 9;
//     float viewDistance = length(viewPos - fragPos);
//     float diskRadius = (1.0 + (viewDistance / far_plane)) / 25.0;
//     for(int i = 0; i < samples; ++i)
//     {
//         // float closestDepth = texture(depthMap, fragToLight + gridSamplingDisk[i] * diskRadius).r;
//         float closestDepth = texture(shadowMap, fragToLight.xy + gridSamplingDisk[i] * diskRadius).r;
//         closestDepth *= far_plane;   // undo mapping [0;1]
//         if(currentDepth - bias > closestDepth)
//             shadow += 1.0;
//     }
//     shadow /= float(samples);
        
//     // display closestDepth as debug (to visualize depth cubemap)
//     // FragColor = vec4(vec3(closestDepth / far_plane), 1.0);    
        
//     return shadow;
// }

void main()
{           
    vec3 color = texture(diffuseTexture, fs_in.TexCoords).rgb;
    vec3 normal = normalize(fs_in.Normal);
    vec3 lightColor = vec3(0.7);
    // ambient
    vec3 ambient = 0.09 * lightColor;
    // diffuse
    vec3 lightDir = normalize(light.position - fs_in.FragPos);
    float diff = max(dot(lightDir, normal), 0.0);
    vec3 diffuse = diff * lightColor;
    // specular
    vec3 viewDir = normalize(viewPos - fs_in.FragPos);
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = 0.0;
    vec3 halfwayDir = normalize(lightDir + viewDir);  
    spec = pow(max(dot(normal, halfwayDir), 0.0), 64.0);
    vec3 specular = spec * lightColor;    

    // Add on spotlight affect (with soft edges)
    // ripped from 2.5.4 light_casters
    float theta = dot(lightDir, normalize(-light.direction));
    float epsilon = (light.cutOff - light.outerCutOff);
    float intensity = clamp((theta - light.outerCutOff) / epsilon, 0.0, 1.0);

    // modify diffuse and specular by intensity. 
    diffuse *= intensity;
    specular *= intensity;
    
    //Let's leave attenuation out
    // for now and see what happens...

    // calculate shadow
    float shadow = shadows ? ShadowCalculation(fs_in.FragPosLightSpace) : 0.0;  
    // float shadow = shadows ? ShadowCalculation(fs_in.FragPos) : 0.0;
                           
    vec3 lighting = (ambient + (1.0 - shadow) * (diffuse + specular)) * color;    
    // vec3 lighting = vec3(texture(gPosition, fs_in.FragPos.xy));
    
    FragColor = vec4(lighting, 1.0);
}