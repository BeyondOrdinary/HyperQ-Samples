import sys
import time
import platform
import datetime as dt
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.ticker as mticker
import matplotlib.dates as mdates
import argparse

def str2bool(v) :
    if isinstance(v,bool) :
        return v
    if v.lower() in ('yes','true','t','y','1') :
        return True
    if v.lower() in ('no','false','f','n','0') :
        return False
    raise argparse.ArgumentTypeError(f"Boolean expected, but did not understand {v}")

def dograph(csvfile,window,outfile) :
    data = pd.read_csv(csvfile,header=0)
    report = []

    surface_hits = data[data['altitude'] <= 0.0]
    avg_landing_reward = surface_hits['reward'].mean()
    min_landing_reward = surface_hits['reward'].min()
    max_landing_reward = surface_hits['reward'].max()
    std_landing_reward = surface_hits['reward'].std()

    avg_landing_speed = surface_hits['velocity'].mean()
    std_landing_speed = surface_hits['velocity'].std()

    print(f'Average reward from hitting the surface: {avg_landing_reward} +/- {std_landing_reward}')
    print(f'Bounds of reward from hitting the surface: [{min_landing_reward} , {max_landing_reward}]')
    print(f'Average speed at surface impact: {avg_landing_speed} +/- {std_landing_speed}')

    report.append(f'[LEM] Average reward from hitting the surface: {avg_landing_reward} +/- {std_landing_reward}')
    report.append(f'[LEM] Bounds of reward from hitting the surface: [{min_landing_reward} , {max_landing_reward}]')
    report.append(f'[LEM] Average speed at surface impact: {avg_landing_speed} +/- {std_landing_speed}')

    fuel_outages = data[data['altitude'] > 0.0]
    avg_fuel_outages = fuel_outages['reward'].mean()
    min_fuel_outages = fuel_outages['reward'].min()
    max_fuel_outages = fuel_outages['reward'].max()
    std_fuel_outages = fuel_outages['reward'].std()

    print(f'Average reward from fuel outage: {avg_fuel_outages} +/- {std_fuel_outages}')
    print(f'Bounds of reward from fuel outage: [{min_fuel_outages} , {max_fuel_outages}]')

    report.append(f'[LEM] Average reward from fuel outage: {avg_fuel_outages} +/- {std_fuel_outages}')
    report.append(f'[LEM] Bounds of reward from fuel outage: [{min_fuel_outages} , {max_fuel_outages}]')

    fillcolor = '#00ffe8'

    spine_color="#5998ff"
    face_color='#07000d'
    top_line_color='#f49842' # '#5642f4'
    top_graph_color='#386d13'
    middle_line_color='#5642f4'
    middle_graph_color='#f442d9'
    h_line_color='#e0e0e0'
    bottom_graph_color='#f4b942'
    bottom_line_color='#5642f4'

    top_color = '#c1f9f7'
    bottom_color = '#f4b942'
    positive_color = '#386d13'
    negative_color = '#8f2020'
    neutral_color = '#f49842'

    plotgrid = (8,4)
    #
    # PLOT 7 rows, 4 columns
    #
    fig = plt.figure(facecolor=face_color,figsize=(22,8))

    ax1 = plt.subplot2grid(plotgrid, (2,0), rowspan=4, colspan=4) # , axisbg='#07000d')
    Label3 = 'Surface Hits Reward'
    reward_x = surface_hits.index.to_numpy()
    reward_y = surface_hits['reward'].to_numpy()
    c1 = ax1.plot(reward_x,reward_y,middle_graph_color,label=Label3, linewidth=1.5)
    ax1.yaxis.label.set_color("w")
    ax1.spines['bottom'].set_color(spine_color)
    ax1.spines['top'].set_color(spine_color)
    ax1.spines['left'].set_color(spine_color)
    ax1.spines['right'].set_color(spine_color)
    ax1.tick_params(axis='y', colors='w')
    ax1.tick_params(axis='x', colors='w')
    ax1.set_ylabel('Reward',color='w')

    ax1r = ax1.twinx()
    reward_x = surface_hits.index.to_numpy()
    reward_y = surface_hits['avg_reward'].to_numpy()
    c2 = ax1r.plot(reward_x,reward_y,middle_line_color,label='Running Avg', linewidth=1.5)
    ax1r.tick_params(axis='y',color=middle_line_color)

    #
    # Top graph is the vertical speed
    #
    ax0 = plt.subplot2grid(plotgrid, (0,0), sharex=ax1, rowspan=2, colspan=4)
    y1 = surface_hits['velocity'].values
    ax0.plot(reward_x, y1, top_graph_color, linewidth=1.1)
    ax0.axhline(1, color=h_line_color)
    ax0.set_yticks([0])
    ax0.yaxis.label.set_color("w")
    ax0.spines['bottom'].set_color(spine_color)
    ax0.spines['top'].set_color(spine_color)
    ax0.spines['left'].set_color(spine_color)
    ax0.spines['right'].set_color(spine_color)
    ax0.tick_params(axis='y', colors='w')
    ax0.tick_params(axis='x', colors='w')
    plt.ylabel(r'V_z')

    y1mean = surface_hits['velocity'].rolling(window).mean().values
    ax0r = ax0.twinx()
    c2 = ax0r.plot(reward_x,y1mean,top_line_color,label='Running Avg', linewidth=1.25)
    ax0r.tick_params(axis='y',color=top_line_color)


    #
    # Second Bottom graph is the reward min/max spread
    #
    ax4 = plt.subplot2grid(plotgrid, (6,0), sharex=ax1, rowspan=1, colspan=4)
    m1 = surface_hits['reward'].rolling(window).max()
    m2 = surface_hits['reward'].rolling(window).min()
    y1 = m1 - m2
    ax4.plot(reward_x, y1.values, top_graph_color, linewidth=1.1)
    ax4.axhline(1, color=h_line_color)
    ax4.set_yticks([0])
    ax4.yaxis.label.set_color("w")
    ax4.spines['bottom'].set_color(spine_color)
    ax4.spines['top'].set_color(spine_color)
    ax4.spines['left'].set_color(spine_color)
    ax4.spines['right'].set_color(spine_color)
    ax4.tick_params(axis='y', colors='w')
    ax4.tick_params(axis='x', colors='w')
    plt.ylabel('Spread')

    y1mean = y1.expanding().mean().values
    ax4r = ax4.twinx()
    c2 = ax4r.plot(reward_x,y1mean,top_line_color,label='Running Avg', linewidth=1.25)
    ax4r.tick_params(axis='y',color=top_line_color)

    #
    # Bottom graph is the fuel level
    #
    ax2 = plt.subplot2grid(plotgrid, (7,0), sharex=ax1, rowspan=1, colspan=4) # , axisbg='#07000d')
    y2 = surface_hits['fuel mass remaining'].values
    ax2.plot(reward_x, y2, bottom_graph_color, linewidth=1.1)
    ax2.axhline(1, color=h_line_color)
    ax2.set_yticks([0])
    ax2.yaxis.label.set_color("w")
    ax2.spines['bottom'].set_color(spine_color)
    ax2.spines['top'].set_color(spine_color)
    ax2.spines['left'].set_color(spine_color)
    ax2.spines['right'].set_color(spine_color)
    ax2.tick_params(axis='y', colors='w')
    ax2.tick_params(axis='x', colors='w')
    ax2.set_ylabel('Fuel',color='w')

    y2mean = surface_hits['fuel mass remaining'].rolling(window).mean().values
    ax2r = ax2.twinx()
    c2 = ax2r.plot(reward_x,y2mean,bottom_line_color,label='Running Avg', linewidth=1.25)
    ax2r.tick_params(axis='y',color=bottom_line_color)

    # Hide the X labels in the top and middle graphs
    plt.suptitle("Rocket2 (LEM) Landing Performance",color='w')
    plt.setp(ax0.get_xticklabels(), visible=False)
    plt.setp(ax1.get_xticklabels(), visible=False)
    plt.setp(ax4.get_xticklabels(), visible=False)

    fig.savefig(outfile, bbox_inches="tight",facecolor=fig.get_facecolor())
    plt.close()
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--incsv", help="the csv file to process",type=str)
    parser.add_argument("--window", help="rolling window size",type=int, default=100)
    parser.add_argument("--outfile", help="name of the output graph file",type=str, default="surface_hits.png")

    opts = parser.parse_args()
    report = dograph(opts.incsv,opts.window,opts.outfile)
