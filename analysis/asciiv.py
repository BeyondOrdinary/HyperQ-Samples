from PIL import Image, ImageDraw, ImageFont
import os
from sys import argv

def parse_log_file(log_file_path, frame_marker=">>frame"):
    """Parse the log file into frames based on the frame marker."""
    with open(log_file_path, 'r') as file:
        log_contents = file.read()
    frames = log_contents.split(frame_marker)
    return [frame.strip() for frame in frames if frame.strip() and frame.find("|") > -1]  # Remove empty frames

def render_frame(frame_content, width=50, height=50, font_size=96, colors={}):
    """Render a single frame as an image."""
    
    # Load a font
    try:
        font = ImageFont.truetype("Courier_New.ttf", font_size)  # Replace with your font path
    except IOError:
        font = ImageFont.load_default()

    standard_width = font.getbbox("W")[2]
    standard_height = font.getbbox("W")[3]

    # Split content into lines
    lines = frame_content.split("\n")
    maxw = standard_width * (6 + len(lines[0]))
    maxh = standard_height * (3 + len(lines))
    if width is None or maxw > width :
        width = maxw
    if height is None or maxh > height :
        height = maxh

    # Create a blank image
    img = Image.new("RGBA", (width, height), "black")
    draw = ImageDraw.Draw(img)

    # Draw each line with optional highlighting
    y_position = 10  # Start at the top
    for line in lines:
        # Highlight specific characters or lines (e.g., 'X' on the board)
        colored_line = []
        for char in line:
            if char in colors.keys():
                colored_line.append((char, colors[char]))
            else:
                colored_line.append((char, (255, 255, 255)))  # Default white color

        # Combine characters into a line
        x_position = 10
        for char, color in colored_line:
            char_width = font.getbbox(char)[2]  # The width of the character
            cxoff = 0
            if char_width < standard_width :
                cxoff = (standard_width - char_width)//2
            draw.text((x_position+cxoff, y_position), char, font=font, fill=color)
            # Use getbbox() to determine character width
            if char_width < standard_width :
                char_width = standard_width
            x_position += char_width

        y_position += standard_height  # Use a standard character height for line spacing

    return img

def create_animated_gif(frames, output_path, duration=350):
    """Create an animated GIF from a list of images."""
    if not frames:
        raise ValueError("No frames to create GIF")
    frames[0].save(
        output_path,
        save_all=True,
        append_images=frames[1:],
        duration=duration,
        loop=0  # Infinite loop
    )

if __name__=="__main__":
    # File paths
    log_file_path = argv[1]  # Replace with the path to your log file
    output_gif_path = argv[2]

    colors = {
        '@' : (255,255,255),
        '$' : (255,215,0),
        'W' : (255,0,0),
        '#' : (153,101,21),
        'O' : (230,190,138),
        '+' : (0,255,0),
        '=' : (205,133,63)
    }
    # Parse log file into frames
    log_frames = parse_log_file(log_file_path)

    # Render frames into images
    image_frames = []
    last_frame = None
    for frame in log_frames :
        if(last_frame is None or last_frame != frame) :
            image_frames.append(render_frame(frame, colors=colors))
            last_frame = frame
#    image_frames = [render_frame(frame,colors=colors) for frame in log_frames]

    # Create animated GIF
    create_animated_gif(image_frames, output_gif_path)

    print(f"Animated GIF created at {output_gif_path}")
