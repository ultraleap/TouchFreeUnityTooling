import classnames from 'classnames/bind';

import styles from './Sliders.module.scss';
import interactionStyles from '@/Pages/Interactions/Interactions.module.scss';

import React, { PointerEvent, RefObject } from 'react';

const classes = classnames.bind(styles);
const interactionClasses = classnames.bind(interactionStyles);

interface TextSliderProps {
    name: string;
    rangeMin: number;
    rangeMax: number;
    leftLabel: string;
    rightLabel: string;
    value: number;
    onChange: (newValue: number) => void;
}

export class TextSlider extends React.Component<TextSliderProps, {}> {
    public static defaultProps = {
        increment: 0.1,
    };

    private dragging = false;
    private stepSize = 0.05;

    private inputElement: RefObject<HTMLInputElement>;

    constructor(props: TextSliderProps) {
        super(props);

        this.inputElement = React.createRef();

        document.body.addEventListener('pointerup', this.onUpCancel.bind(this));
        document.body.addEventListener('pointercancel', this.onUpCancel.bind(this));
    }

    private onChange() {
        // this function is here purely to pass to the input, preventing it becoming ReadOnly
    }
    private onTextChange(e: React.FormEvent<HTMLInputElement>): void {
        const hoverStartTime: number = Number.parseFloat(e.currentTarget.value);
        this.props.onChange(hoverStartTime);
    }

    private onUpCancel() {
        this.dragging = false;
    }

    private onDown(event: PointerEvent<HTMLInputElement>) {
        this.dragging = true;
        this.setValueByPos(event.nativeEvent.offsetX);
    }

    private onMove(event: PointerEvent<HTMLInputElement>) {
        if (this.dragging) {
            this.setValueByPos(event.nativeEvent.offsetX);
        }
    }

    private setValueByPos(xPos: number) {
        if (this.inputElement.current !== null) {
            // Slider height is currently 0.75rem
            const remValue = this.inputElement.current.clientHeight;

            // Slider control is 1.5rem wide, so half is 1x remValue, full is 2x remValue
            const posInRange: number = (xPos - remValue) / (this.inputElement.current.clientWidth - 2 * remValue);
            const outputValue: number = this.lerp(this.props.rangeMin, this.props.rangeMax, posInRange);
            const roundedValue = Math.round(outputValue * (1 / this.stepSize)) / (1 / this.stepSize);

            if (this.props.rangeMin <= roundedValue && roundedValue <= this.props.rangeMax) {
                this.props.onChange(roundedValue);
            }
        }
    }

    private lerp(v0: number, v1: number, t: number): number {
        return v0 * (1 - t) + v1 * t;
    }

    render() {
        return (
            <label className={interactionClasses('input-label-container')}>
                <p className={interactionClasses('label')}>{this.props.name}</p>
                <div className={classes('sliderContainer')}>
                    <input
                        type="range"
                        step={this.stepSize}
                        min={this.props.rangeMin}
                        max={this.props.rangeMax}
                        value={this.props.value}
                        className={classes('slider')}
                        onChange={this.onChange}
                        onPointerMove={this.onMove.bind(this)}
                        onPointerDown={this.onDown.bind(this)}
                        onPointerUp={this.onUpCancel.bind(this)}
                        onPointerCancel={this.onUpCancel.bind(this)}
                        id="myRange"
                        ref={this.inputElement}
                    />
                    <div className={classes('sliderLabelContainer')}>
                        <label className={interactionClasses('leftLabel')}>{this.props.leftLabel}</label>
                        <label className={interactionClasses('rightLabel')}>{this.props.rightLabel}</label>
                    </div>
                </div>
                <label className={classes('sliderTextContainer')}>
                    <input
                        type="number"
                        step={this.stepSize}
                        className={classes('sliderText')}
                        value={this.props.value}
                        onChange={this.onTextChange.bind(this)}
                    />
                </label>
            </label>
        );
    }
}
