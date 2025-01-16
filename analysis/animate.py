from PIL import Image, ImageDraw, ImageFont
import os
from sys import argv
import re

def animate(folder, png_files = None,output_path = "hunt_learner.gif") :
    # Specify the folder containing PNG images
    image_folder = folder

    # Collect all PNG files in the folder
    if png_files is None :
        png_files = [os.path.join(image_folder, f) for f in os.listdir(image_folder) if f.endswith('.png')]
        # Sort the files to maintain the correct sequence
        png_files.sort()

    # Open images and convert them to RGBA mode (to handle transparency if needed)
    frames = []

    # Load a font (you can download and specify a .ttf font file if required)
    font_size = 30
    try:
        font = ImageFont.truetype("arial.ttf", font_size)  # Replace with a valid font path if needed
    except IOError:
        font = ImageFont.load_default()

    for i, png_file in enumerate(png_files):
        if png_file.endswith('.csv') :
            png_file = png_file.replace(".csv","_heatmap.png")
        img = Image.open(png_file).convert("RGBA")  # Convert to RGBA mode
        draw = ImageDraw.Draw(img)
        
        # Define text position and content
        text = f"Frame {i + 1}"
        nn = re.findall(r'\d+',png_file)
        if not nn is None and len(nn) > 0 :
            text = f'Episode {nn[0]}'
        print(text)
        text_position = (10, 10)  # Top-left corner
        
        # Add text to the image
        draw.text(text_position, text, font=font, fill=(0, 0, 255, 255))  # blue text
        
        frames.append(img) 

    # Save as an animated GIF
    frames[0].save(
        output_path,
        save_all=True,
        append_images=frames[1:],  # Add the other frames
        duration=550,  # Duration for each frame in milliseconds
        loop=0  # Loop 0 means infinite looping
    )

    print(f"Animated GIF saved at {output_path}")
    return output_path

if __name__=="__main__":
    files = None
    if len(argv) == 3 :
        files = []
        with open(argv[2],"r") as fp :
            lines = fp.readlines()
            for s in lines :
                s = s.strip()
                if len(s) == 0 or s[0] == '#':
                    continue
                files.append(s)
    animate(argv[1], files)
