import 'Styles/Camera/CameraMasking.scss';

import React, { PointerEvent, useRef, useState } from 'react';

export type SliderDirection = 'left' | 'right' | 'top' | 'bottom';

interface SliderPosInfo {
    X: number;
    Y: number;
    initialTop: number;
    initialLeft: number;
}

const MaskingSlider: React.FC<{ direction: SliderDirection }> = ({ direction }) => {
    const [isHovered, setIsHovered] = useState<boolean>(false);
    const [isDragging, setIsDragging] = useState<boolean>(false);

    const startPosInfo = useRef<SliderPosInfo>({
        X: Number.NaN,
        Y: Number.NaN,
        initialTop: Number.NaN,
        initialLeft: Number.NaN,
    });

    const sliderRef = useRef<HTMLSpanElement>(null);

    const onStartDrag = (event: PointerEvent<HTMLSpanElement>) => {
        if (!sliderRef.current) return;

        console.log('START');
        console.log('INNER W: ' + window.innerWidth);
        console.log('INNER H: ' + window.innerHeight);
        console.log('OUTER W: ' + window.outerWidth);
        console.log('OUTER H: ' + window.outerHeight);

        setIsDragging(true);
        if (Number.isNaN(startPosInfo.current.X) || Number.isNaN(startPosInfo.current.Y)) {
            const top = Number.parseFloat(getComputedStyle(sliderRef.current).top);
            const left = Number.parseFloat(getComputedStyle(sliderRef.current).left);

            startPosInfo.current = { X: event.pageX, Y: event.pageY, initialTop: top, initialLeft: left };
        }

        window.addEventListener('pointermove', onMove);
        window.addEventListener('pointerup', onEndDrag);
    };

    const onMove = (event: globalThis.PointerEvent) => {
        if (!sliderRef.current) return;

        console.log('onMove');

        switch (direction) {
            case 'left':
                sliderRef.current.style.left = `${
                    startPosInfo.current.initialLeft + limitVal(event.pageX - startPosInfo.current.X)
                }px`;
                break;
            case 'right':
                sliderRef.current.style.left = `${
                    startPosInfo.current.initialLeft - limitVal(startPosInfo.current.X - event.pageX)
                }px`;
                break;
            case 'top':
                sliderRef.current.style.top = `${
                    startPosInfo.current.initialTop + limitVal(event.pageY - startPosInfo.current.Y)
                }px`;
                break;
            case 'bottom':
                sliderRef.current.style.top = `${
                    startPosInfo.current.initialTop - limitVal(startPosInfo.current.Y - event.pageY)
                }px`;
                break;
        }
    };

    const onEndDrag = () => {
        console.log('UP');
        setIsDragging(false);
        window.removeEventListener('pointermove', onMove);
        window.removeEventListener('pointerup', onEndDrag);
    };

    return (
        <span ref={sliderRef} className={`masking-slider--${direction}`}>
            <div
                className={`masking-slider-knob ${isHovered || isDragging ? 'masking-slider-knob--dragging' : ''}`}
                onPointerDown={onStartDrag}
                onPointerEnter={() => setIsHovered(true)}
                onPointerLeave={() => setIsHovered(false)}
            >
                <div className="masking-slider-arrow--one" />
                <div className="masking-slider-arrow--two" />
            </div>
        </span>
    );
};

const limitVal = (val: number): number => Math.min(400, Math.max(0, val));

export default MaskingSlider;
